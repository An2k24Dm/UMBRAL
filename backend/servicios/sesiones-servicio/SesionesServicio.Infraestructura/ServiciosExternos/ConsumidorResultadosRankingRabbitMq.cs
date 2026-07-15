using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SesionesServicio.Aplicacion.Comandos.AplicarPuntajeRanking;

namespace SesionesServicio.Infraestructura.ServiciosExternos;

public sealed class ConsumidorResultadosRankingRabbitMq : BackgroundService
{
    private const string RoutingKeyPuntajeActualizado = "ranking.puntaje_actualizado";
    private const string SufijoDlq = ".dlq";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OpcionesRabbitMq _opciones;
    private readonly ILogger<ConsumidorResultadosRankingRabbitMq> _log;
    private IConnection? _conexion;
    private IModel? _canal;
    private TaskCompletionSource _desconexion =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public ConsumidorResultadosRankingRabbitMq(
        IServiceScopeFactory scopeFactory,
        IOptions<OpcionesRabbitMq> opciones,
        ILogger<ConsumidorResultadosRankingRabbitMq> log)
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
                _log.LogInformation(
                    "ConsumidorResultadosRanking conectado a RabbitMQ, cola '{Cola}'",
                    _opciones.ColaResultadosRanking);
                await _desconexion.Task.WaitAsync(stoppingToken);
                _log.LogWarning(
                    "ConsumidorResultadosRanking detecto cierre de conexion/canal RabbitMQ. Reintentando conexion...");
                LimpiarConexion();
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex,
                    "Error en ConsumidorResultadosRanking, reintentando en 10s...");
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

        _conexion = fabrica.CreateConnection("sesiones-servicio-ranking-resultados");
        _canal = _conexion.CreateModel();
        _conexion.ConnectionShutdown += (_, args) =>
        {
            _log.LogWarning(
                "Conexion RabbitMQ de resultados Ranking cerrada. ReplyCode={ReplyCode} ReplyText={ReplyText}",
                args.ReplyCode,
                args.ReplyText);
            _desconexion.TrySetResult();
        };
        _canal.ModelShutdown += (_, args) =>
        {
            _log.LogWarning(
                "Canal RabbitMQ de resultados Ranking cerrado. ReplyCode={ReplyCode} ReplyText={ReplyText}",
                args.ReplyCode,
                args.ReplyText);
            _desconexion.TrySetResult();
        };
        _canal.ExchangeDeclare(
            _opciones.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        _canal.QueueDeclare(
            _opciones.ColaResultadosRanking, durable: true, exclusive: false, autoDelete: false);
        _canal.QueueDeclare(
            _opciones.ColaResultadosRanking + SufijoDlq,
            durable: true,
            exclusive: false,
            autoDelete: false);
        _canal.QueueBind(
            _opciones.ColaResultadosRanking, _opciones.Exchange, RoutingKeyPuntajeActualizado);
        _canal.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumidor = new AsyncEventingBasicConsumer(_canal);
        consumidor.Received += OnMensajeRecibidoAsync;
        _canal.BasicConsume(_opciones.ColaResultadosRanking, autoAck: false, consumidor);
    }

    private async Task OnMensajeRecibidoAsync(object sender, BasicDeliverEventArgs args)
    {
        var cuerpo = Encoding.UTF8.GetString(args.Body.ToArray());
        try
        {
            var ev = JsonSerializer.Deserialize<EventoPuntajeActualizadoRanking>(
                cuerpo, OpcionesJson)!;

            _log.LogInformation(
                "Resultado de Ranking recibido. RoutingKey={RoutingKey} EventoIdOrigen={EventoIdOrigen} SesionId={SesionId} ParticipanteSesionId={ParticipanteSesionId} ParticipanteIdentidadId={ParticipanteIdentidadId} EquipoId={EquipoId}",
                args.RoutingKey,
                ev.EventoIdOrigen,
                ev.SesionId,
                ev.ParticipanteSesionId,
                ev.ParticipanteIdentidadId,
                ev.EquipoId);

            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new AplicarPuntajeRankingComando(
                ev.EventoIdOrigen,
                ev.SesionId,
                ev.ParticipanteSesionId,
                ev.ParticipanteIdentidadId,
                ev.EquipoId,
                ev.PuntajeGanado,
                ev.PuntajeTotalParticipante,
                ev.PuntajeTotalEquipo,
                ev.CalculadoEnUtc));

            _canal?.BasicAck(args.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "Error procesando resultado de Ranking. Body={Body}",
                cuerpo);
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
            routingKey: _opciones.ColaResultadosRanking + SufijoDlq,
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(cuerpo));
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

    private sealed record EventoPuntajeActualizadoRanking(
        Guid EventoIdOrigen,
        Guid SesionId,
        Guid ParticipanteSesionId,
        Guid ParticipanteIdentidadId,
        Guid? EquipoId,
        long PuntajeGanado,
        long PuntajeTotalParticipante,
        long? PuntajeTotalEquipo,
        DateTime CalculadoEnUtc);

    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
