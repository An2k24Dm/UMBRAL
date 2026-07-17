using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RankingServicio.Aplicacion.Comandos.ProcesarEvidenciaTesoro;
using RankingServicio.Aplicacion.Comandos.ProcesarEquipoCreado;
using RankingServicio.Aplicacion.Comandos.ProcesarParticipanteUnido;
using RankingServicio.Aplicacion.Comandos.ProcesarPenalizacion;
using RankingServicio.Aplicacion.Comandos.ProcesarRespuestaTrivia;
using RankingServicio.Commons.Dtos.Eventos.Entrada;

namespace RankingServicio.Infraestructura.RabbitMq;

public sealed class ConsumidorEventosRanking : BackgroundService
{
    private const string RoutingKeyTrivia = "sesion.respuesta_trivia";
    private const string RoutingKeyTesoro = "sesion.evidencia_tesoro";
    private const string RoutingKeyParticipante = "sesion.participante_unido";
    private const string RoutingKeyEquipo = "sesion.equipo_creado";
    private const string RoutingKeyPenalizacion = "sesion.penalizacion_aplicada";
    private const string SufijoDlq = ".dlq";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OpcionesRabbitMq _opciones;
    private readonly ILogger<ConsumidorEventosRanking> _log;

    private IConnection? _conexion;
    private IModel? _canal;
    private TaskCompletionSource _desconexion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ConsumidorEventosRanking(
        IServiceScopeFactory scopeFactory,
        IOptions<OpcionesRabbitMq> opciones,
        ILogger<ConsumidorEventosRanking> log)
    {
        _scopeFactory = scopeFactory;
        _opciones = opciones.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                IniciarConexion();
                _log.LogInformation("ConsumidorRanking conectado a RabbitMQ, escuchando cola '{Cola}'", _opciones.Cola);
                await _desconexion.Task.WaitAsync(stoppingToken);
                _log.LogWarning("ConsumidorRanking detecto cierre de conexion/canal RabbitMQ. Reintentando conexion...");
                LimpiarConexion();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Error en ConsumidorRanking, reintentando en 10s...");
                LimpiarConexion();
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private void IniciarConexion()
    {
        LimpiarConexion();
        _desconexion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var fabrica = new ConnectionFactory
        {
            HostName = _opciones.Host,
            Port = _opciones.Puerto,
            UserName = _opciones.Usuario,
            Password = _opciones.Contrasena,
            DispatchConsumersAsync = true
        };

        _conexion = fabrica.CreateConnection("ranking-servicio");
        _canal = _conexion.CreateModel();
        _conexion.ConnectionShutdown += (_, args) =>
        {
            _log.LogWarning(
                "Conexion RabbitMQ de Ranking cerrada. ReplyCode={ReplyCode} ReplyText={ReplyText}",
                args.ReplyCode,
                args.ReplyText);
            _desconexion.TrySetResult();
        };
        _canal.ModelShutdown += (_, args) =>
        {
            _log.LogWarning(
                "Canal RabbitMQ de Ranking cerrado. ReplyCode={ReplyCode} ReplyText={ReplyText}",
                args.ReplyCode,
                args.ReplyText);
            _desconexion.TrySetResult();
        };

        _canal.ExchangeDeclare(
            _opciones.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        _canal.QueueDeclare(
            _opciones.Cola, durable: true, exclusive: false, autoDelete: false);
        _canal.QueueDeclare(
            _opciones.Cola + SufijoDlq, durable: true, exclusive: false, autoDelete: false);

        foreach (var routingKey in new[]
        {
            RoutingKeyTrivia, RoutingKeyTesoro,
            RoutingKeyParticipante, RoutingKeyEquipo,
            RoutingKeyPenalizacion
        })
        {
            _canal.QueueBind(_opciones.Cola, _opciones.Exchange, routingKey);
        }

        _canal.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumidor = new AsyncEventingBasicConsumer(_canal);
        consumidor.Received += OnMensajeRecibidoAsync;

        _canal.BasicConsume(_opciones.Cola, autoAck: false, consumidor);
    }

    private async Task OnMensajeRecibidoAsync(object sender, BasicDeliverEventArgs args)
    {
        var routingKey = args.RoutingKey;
        var cuerpo = Encoding.UTF8.GetString(args.Body.ToArray());

        try
        {
            await ProcesarAsync(routingKey, cuerpo);
            _canal?.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "Error procesando mensaje RabbitMQ. RoutingKey={RoutingKey} Body={Body}",
                routingKey, cuerpo);
            if (!args.Redelivered)
            {
                _canal?.BasicNack(args.DeliveryTag, multiple: false, requeue: true);
                return;
            }

            PublicarDeadLetter(args, cuerpo, ex);
            _canal?.BasicAck(args.DeliveryTag, multiple: false);
        }
    }

    private void PublicarDeadLetter(BasicDeliverEventArgs args, string cuerpo, Exception ex)
    {
        if (_canal is null) return;

        var props = _canal.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.Headers = new Dictionary<string, object>
        {
            ["x-error"] = ex.Message,
            ["x-original-routing-key"] = args.RoutingKey,
            ["x-failed-at-utc"] = DateTime.UtcNow.ToString("O")
        };

        _canal.BasicPublish(
            exchange: string.Empty,
            routingKey: _opciones.Cola + SufijoDlq,
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(cuerpo));
    }

    private async Task ProcesarAsync(string routingKey, string cuerpo)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        switch (routingKey)
        {
            case RoutingKeyTrivia:
            {
                var ev = JsonSerializer.Deserialize<EventoRespuestaTriviaRegistrada>(cuerpo,
                    OpcionesJson)!;
                RegistrarEventoRecibido(
                    routingKey, ev.EventoId, ev.SesionId,
                    ev.ParticipanteSesionId, ev.ParticipanteIdentidadId, ev.EquipoId);
                await mediator.Send(new ProcesarRespuestaTriviaComando(
                    ev.EventoId, ev.SesionId, ev.MisionId, ev.EtapaId,
                    ev.ParticipanteSesionId, ev.ParticipanteIdentidadId, ev.EquipoId,
                    ev.TriviaId, ev.PreguntaId, ev.EsCorrecta, ev.PuntajeBase,
                    ev.TiempoTardadoMs, ev.TiempoLimiteMs));
                break;
            }
            case RoutingKeyTesoro:
            {
                var ev = JsonSerializer.Deserialize<EventoEvidenciaTesoroRegistrada>(cuerpo,
                    OpcionesJson)!;
                RegistrarEventoRecibido(
                    routingKey, ev.EventoId, ev.SesionId,
                    ev.ParticipanteSesionId, ev.ParticipanteIdentidadId, ev.EquipoId);
                await mediator.Send(new ProcesarEvidenciaTesoroComando(
                    ev.EventoId, ev.SesionId, ev.MisionId, ev.EtapaId,
                    ev.ParticipanteSesionId, ev.ParticipanteIdentidadId, ev.EquipoId,
                    ev.BusquedaId, ev.EsValida, ev.PuntajeBase,
                    ev.OrdenResolucion, ev.TotalCompetidores,
                    ev.TiempoTranscurridoMs, ev.TiempoLimiteMs));
                break;
            }
            case RoutingKeyParticipante:
            {
                var ev = JsonSerializer.Deserialize<EventoParticipanteUnidoSesion>(cuerpo,
                    OpcionesJson)!;
                RegistrarEventoRecibido(
                    routingKey, ev.EventoId, ev.SesionId,
                    ev.ParticipanteSesionId, ev.ParticipanteIdentidadId, ev.EquipoId);
                await mediator.Send(new ProcesarParticipanteUnidoComando(
                    ev.EventoId, ev.SesionId, ev.ParticipanteSesionId,
                    ev.ParticipanteIdentidadId, ev.EquipoId));
                break;
            }
            case RoutingKeyEquipo:
            {
                var ev = JsonSerializer.Deserialize<EventoEquipoCreadoSesion>(cuerpo,
                    OpcionesJson)!;
                _log.LogInformation(
                    "Evento recibido en Ranking. RoutingKey={RoutingKey} EventoId={EventoId} SesionId={SesionId} EquipoId={EquipoId}",
                    routingKey,
                    ev.EventoId,
                    ev.SesionId,
                    ev.EquipoId);
                await mediator.Send(new ProcesarEquipoCreadoComando(
                    ev.EventoId, ev.SesionId, ev.EquipoId));
                break;
            }
            case RoutingKeyPenalizacion:
            {
                var ev = JsonSerializer.Deserialize<EventoPenalizacionAplicada>(cuerpo,
                    OpcionesJson)!;
                _log.LogInformation(
                    "Penalización recibida en Ranking. RoutingKey={RoutingKey} EventoId={EventoId} SesionId={SesionId} TipoObjetivo={TipoObjetivo} ParticipanteSesionId={ParticipanteSesionId} EquipoId={EquipoId} Puntos={Puntos}",
                    routingKey,
                    ev.EventoId,
                    ev.SesionId,
                    ev.TipoObjetivo,
                    ev.ParticipanteSesionId,
                    ev.EquipoId,
                    ev.Puntos);
                await mediator.Send(new ProcesarPenalizacionComando(
                    ev.EventoId, ev.SesionId, ev.TipoObjetivo,
                    ev.ParticipanteSesionId, ev.ParticipanteIdentidadId, ev.EquipoId,
                    ev.Puntos, ev.Motivo, ev.OperadorIdentidadId, ev.AplicadaEnUtc));
                break;
            }
            default:
                _log.LogWarning("Mensaje con routing key desconocido: {RoutingKey}", routingKey);
                break;
        }
    }

    private void RegistrarEventoRecibido(
        string routingKey,
        Guid eventoId,
        Guid sesionId,
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        Guid? equipoId)
        => _log.LogInformation(
            "Evento recibido en Ranking. RoutingKey={RoutingKey} EventoId={EventoId} SesionId={SesionId} ParticipanteSesionId={ParticipanteSesionId} ParticipanteIdentidadId={ParticipanteIdentidadId} EquipoId={EquipoId}",
            routingKey,
            eventoId,
            sesionId,
            participanteSesionId,
            participanteIdentidadId,
            equipoId);

    private void LimpiarConexion()
    {
        try { _canal?.Close(); } catch { }
        try { _conexion?.Close(); } catch { }
        _canal = null;
        _conexion = null;
    }

    public override void Dispose()
    {
        LimpiarConexion();
        base.Dispose();
    }

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
