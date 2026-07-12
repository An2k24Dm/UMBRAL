using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Infraestructura.ServiciosExternos;

public sealed class PublicadorEventosRankingRabbitMq : IPublicadorEventosRanking, IDisposable
{
    private const string RoutingKeyTrivia = "sesion.respuesta_trivia";
    private const string RoutingKeyTesoro = "sesion.evidencia_tesoro";
    private const string RoutingKeyFinalizada = "sesion.finalizada";
    private const string RoutingKeyParticipante = "sesion.participante_unido";
    private const string RoutingKeyEquipo = "sesion.equipo_creado";

    private readonly OpcionesRabbitMq _opciones;
    private readonly ILogger<PublicadorEventosRankingRabbitMq> _log;
    private IConnection? _conexion;
    private IModel? _canal;
    private readonly object _lock = new();

    public PublicadorEventosRankingRabbitMq(
        IOptions<OpcionesRabbitMq> opciones,
        ILogger<PublicadorEventosRankingRabbitMq> log)
    {
        _opciones = opciones.Value;
        _log = log;
    }

    public Task PublicarRespuestaTriviaRegistradaAsync(
        Guid sesionId, Guid participanteIdentidadId, string nombreParticipante,
        Guid? equipoId, string? nombreEquipo, int puntaje, bool esCorrecta,
        CancellationToken cancelacion)
        => PublicarAsync(RoutingKeyTrivia, new
        {
            EventoId = Guid.NewGuid(),
            SesionId = sesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            NombreParticipante = nombreParticipante,
            EquipoId = equipoId,
            NombreEquipo = nombreEquipo,
            Puntaje = puntaje,
            EsCorrecta = esCorrecta
        });

    public Task PublicarEvidenciaTesoroRegistradaAsync(
        Guid sesionId, Guid participanteIdentidadId, string nombreParticipante,
        Guid? equipoId, string? nombreEquipo, int puntaje,
        CancellationToken cancelacion)
        => PublicarAsync(RoutingKeyTesoro, new
        {
            EventoId = Guid.NewGuid(),
            SesionId = sesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            NombreParticipante = nombreParticipante,
            EquipoId = equipoId,
            NombreEquipo = nombreEquipo,
            Puntaje = puntaje
        });

    public Task PublicarSesionFinalizadaAsync(
        Guid sesionId, bool esGrupal, CancellationToken cancelacion)
        => PublicarAsync(RoutingKeyFinalizada, new
        {
            EventoId = Guid.NewGuid(),
            SesionId = sesionId,
            EsGrupal = esGrupal
        });

    public Task PublicarParticipanteUnidoSesionAsync(
        Guid sesionId, Guid participanteIdentidadId, string nombreParticipante,
        CancellationToken cancelacion)
        => PublicarAsync(RoutingKeyParticipante, new
        {
            EventoId = Guid.NewGuid(),
            SesionId = sesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            NombreParticipante = nombreParticipante
        });

    public Task PublicarEquipoCreadoSesionAsync(
        Guid sesionId, Guid equipoId, string nombreEquipo,
        CancellationToken cancelacion)
        => PublicarAsync(RoutingKeyEquipo, new
        {
            EventoId = Guid.NewGuid(),
            SesionId = sesionId,
            EquipoId = equipoId,
            NombreEquipo = nombreEquipo
        });

    private Task PublicarAsync(string routingKey, object payload)
    {
        try
        {
            var canal = ObtenerCanal();
            var cuerpo = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            var props = canal.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";
            canal.BasicPublish(_opciones.Exchange, routingKey, props, cuerpo);
        }
        catch (Exception ex)
        {
            _log.LogError(ex,
                "Error publicando evento RabbitMQ con routing key '{RoutingKey}'", routingKey);
        }

        return Task.CompletedTask;
    }

    private IModel ObtenerCanal()
    {
        lock (_lock)
        {
            if (_canal is { IsOpen: true }) return _canal;

            var fabrica = new ConnectionFactory
            {
                HostName = _opciones.Host,
                Port = _opciones.Puerto,
                UserName = _opciones.Usuario,
                Password = _opciones.Contrasena
            };

            _conexion = fabrica.CreateConnection("sesiones-servicio-publisher");
            _canal = _conexion.CreateModel();
            _canal.ExchangeDeclare(
                _opciones.Exchange, ExchangeType.Topic, durable: true, autoDelete: false);

            return _canal;
        }
    }

    public void Dispose()
    {
        try { _canal?.Close(); } catch { }
        try { _conexion?.Close(); } catch { }
    }
}
