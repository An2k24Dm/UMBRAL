using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.Infraestructura.ServiciosExternos;

public sealed class DespachadorOutboxRankingRabbitMq : BackgroundService
{
    private const int MaxIntentos = 8;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OpcionesRabbitMq _opciones;
    private readonly ILogger<DespachadorOutboxRankingRabbitMq> _log;
    private IConnection? _conexion;
    private IModel? _canal;

    public DespachadorOutboxRankingRabbitMq(
        IServiceScopeFactory scopeFactory,
        IOptions<OpcionesRabbitMq> opciones,
        ILogger<DespachadorOutboxRankingRabbitMq> log)
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
                await DespacharPendientesAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _log.LogWarning(ex, "Error despachando outbox de Sesiones hacia Ranking.");
                LimpiarConexion();
            }

            await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
        }
    }

    private async Task DespacharPendientesAsync(CancellationToken cancelacion)
    {
        using var scope = _scopeFactory.CreateScope();
        var contexto = scope.ServiceProvider.GetRequiredService<ContextoSesiones>();
        var ahora = DateTime.UtcNow;
        var pendientes = await contexto.OutboxRanking
            .Where(m => m.Estado == "Pendiente"
                && (!m.ProximoIntentoUtc.HasValue || m.ProximoIntentoUtc <= ahora))
            .OrderBy(m => m.CreadoEnUtc)
            .Take(25)
            .ToListAsync(cancelacion);

        foreach (var mensaje in pendientes)
        {
            try
            {
                Publicar(mensaje);
                mensaje.Estado = "Enviado";
                mensaje.EnviadoEnUtc = DateTime.UtcNow;
                mensaje.UltimoError = null;
            }
            catch (Exception ex)
            {
                mensaje.Intentos++;
                mensaje.UltimoError = ex.Message.Length > 1000
                    ? ex.Message[..1000]
                    : ex.Message;
                mensaje.ProximoIntentoUtc = DateTime.UtcNow.AddSeconds(
                    Math.Min(300, Math.Pow(2, mensaje.Intentos)));
                if (mensaje.Intentos >= MaxIntentos)
                {
                    mensaje.Estado = "Fallido";
                    _log.LogError(ex,
                        "Outbox Sesiones->Ranking enviado a estado Fallido. EventoId={EventoId} RoutingKey={RoutingKey}",
                        mensaje.Id, mensaje.RoutingKey);
                }
                else
                {
                    _log.LogWarning(ex,
                        "Fallo publicando outbox Sesiones->Ranking. EventoId={EventoId} Intento={Intento}",
                        mensaje.Id, mensaje.Intentos);
                }
            }
        }

        if (pendientes.Count > 0)
            await contexto.SaveChangesAsync(cancelacion);
    }

    private void Publicar(OutboxMensajeRankingModelo mensaje)
    {
        var canal = ObtenerCanal();
        var props = canal.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.MessageId = mensaje.Id.ToString();
        canal.BasicPublish(
            _opciones.Exchange,
            mensaje.RoutingKey,
            mandatory: true,
            basicProperties: props,
            body: Encoding.UTF8.GetBytes(mensaje.PayloadJson));
        canal.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
    }

    private IModel ObtenerCanal()
    {
        if (_canal is { IsOpen: true }) return _canal;

        var fabrica = new ConnectionFactory
        {
            HostName = _opciones.Host,
            Port = _opciones.Puerto,
            UserName = _opciones.Usuario,
            Password = _opciones.Contrasena
        };

        _conexion = fabrica.CreateConnection("sesiones-servicio-outbox-ranking");
        _canal = _conexion.CreateModel();
        _canal.ExchangeDeclare(
            _opciones.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);
        _canal.ConfirmSelect();
        return _canal;
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
}
