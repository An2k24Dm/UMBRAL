using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RankingServicio.Aplicacion.Comandos.ProcesarEquipoCreado;
using RankingServicio.Aplicacion.Comandos.ProcesarParticipanteUnido;
using RankingServicio.Aplicacion.Comandos.ProcesarPuntaje;
using RankingServicio.Aplicacion.Comandos.ProcesarSesionFinalizada;

namespace RankingServicio.Infraestructura.RabbitMq;

public sealed class ConsumidorEventosRanking : BackgroundService
{
    private const string RoutingKeyTrivia = "sesion.respuesta_trivia";
    private const string RoutingKeyTesoro = "sesion.evidencia_tesoro";
    private const string RoutingKeyFinalizada = "sesion.finalizada";
    private const string RoutingKeyParticipante = "sesion.participante_unido";
    private const string RoutingKeyEquipo = "sesion.equipo_creado";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OpcionesRabbitMq _opciones;
    private readonly ILogger<ConsumidorEventosRanking> _log;

    private IConnection? _conexion;
    private IModel? _canal;

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
                await Task.Delay(Timeout.Infinite, stoppingToken);
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

        _canal.ExchangeDeclare(
            _opciones.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

        _canal.QueueDeclare(
            _opciones.Cola, durable: true, exclusive: false, autoDelete: false);

        foreach (var routingKey in new[]
        {
            RoutingKeyTrivia, RoutingKeyTesoro, RoutingKeyFinalizada,
            RoutingKeyParticipante, RoutingKeyEquipo
        })
        {
            _canal.QueueBind(_opciones.Cola, _opciones.Exchange, routingKey);
        }

        _canal.BasicQos(prefetchSize: 0, prefetchCount: 10, global: false);

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
            _canal?.BasicNack(args.DeliveryTag, multiple: false, requeue: false);
        }
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
                await mediator.Send(new ProcesarPuntajeComando(
                    ev.EventoId, ev.SesionId, ev.ParticipanteIdentidadId,
                    ev.NombreParticipante, ev.EquipoId, ev.NombreEquipo,
                    ev.Puntaje, ev.EsCorrecta, "Trivia"));
                break;
            }
            case RoutingKeyTesoro:
            {
                var ev = JsonSerializer.Deserialize<EventoEvidenciaTesoroRegistrada>(cuerpo,
                    OpcionesJson)!;
                await mediator.Send(new ProcesarPuntajeComando(
                    ev.EventoId, ev.SesionId, ev.ParticipanteIdentidadId,
                    ev.NombreParticipante, ev.EquipoId, ev.NombreEquipo,
                    ev.Puntaje, true, "Tesoro"));
                break;
            }
            case RoutingKeyFinalizada:
            {
                var ev = JsonSerializer.Deserialize<EventoSesionFinalizada>(cuerpo,
                    OpcionesJson)!;
                await mediator.Send(new ProcesarSesionFinalizadaComando(
                    ev.EventoId, ev.SesionId, ev.EsGrupal));
                break;
            }
            case RoutingKeyParticipante:
            {
                var ev = JsonSerializer.Deserialize<EventoParticipanteUnidoSesion>(cuerpo,
                    OpcionesJson)!;
                await mediator.Send(new ProcesarParticipanteUnidoComando(
                    ev.EventoId, ev.SesionId, ev.ParticipanteIdentidadId, ev.NombreParticipante));
                break;
            }
            case RoutingKeyEquipo:
            {
                var ev = JsonSerializer.Deserialize<EventoEquipoCreadoSesion>(cuerpo,
                    OpcionesJson)!;
                await mediator.Send(new ProcesarEquipoCreadoComando(
                    ev.EventoId, ev.SesionId, ev.EquipoId, ev.NombreEquipo));
                break;
            }
            default:
                _log.LogWarning("Mensaje con routing key desconocido: {RoutingKey}", routingKey);
                break;
        }
    }

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
