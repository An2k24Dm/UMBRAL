using System.Text.Json;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.Infraestructura.ServiciosExternos;

public sealed class PublicadorEventosRankingOutbox : IPublicadorEventosRanking
{
    private const string RoutingKeyTrivia = "sesion.respuesta_trivia";
    private const string RoutingKeyTesoro = "sesion.evidencia_tesoro";
    private const string RoutingKeyParticipante = "sesion.participante_unido";
    private const string RoutingKeyEquipo = "sesion.equipo_creado";

    private readonly ContextoSesiones _contexto;

    public PublicadorEventosRankingOutbox(ContextoSesiones contexto)
    {
        _contexto = contexto;
    }

    public Task PublicarRespuestaTriviaRegistradaAsync(
        Guid eventoId,
        Guid sesionId, Guid misionId, Guid etapaId,
        Guid participanteSesionId, Guid participanteIdentidadId,
        Guid? equipoId, Guid triviaId, Guid preguntaId, bool esCorrecta,
        int puntajeBase, int tiempoTardadoMs, int tiempoLimiteMs,
        CancellationToken cancelacion)
        => EncolarAsync(eventoId, RoutingKeyTrivia, new
        {
            EventoId = eventoId,
            SesionId = sesionId,
            MisionId = misionId,
            EtapaId = etapaId,
            ParticipanteSesionId = participanteSesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            TriviaId = triviaId,
            PreguntaId = preguntaId,
            EsCorrecta = esCorrecta,
            PuntajeBase = puntajeBase,
            TiempoTardadoMs = tiempoTardadoMs,
            TiempoLimiteMs = tiempoLimiteMs
        }, cancelacion);

    public Task PublicarEvidenciaTesoroRegistradaAsync(
        Guid eventoId,
        Guid sesionId, Guid misionId, Guid etapaId,
        Guid participanteSesionId, Guid participanteIdentidadId,
        Guid? equipoId, Guid busquedaId, bool esValida,
        int puntajeBase, CancellationToken cancelacion)
        => EncolarAsync(eventoId, RoutingKeyTesoro, new
        {
            EventoId = eventoId,
            SesionId = sesionId,
            MisionId = misionId,
            EtapaId = etapaId,
            ParticipanteSesionId = participanteSesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            BusquedaId = busquedaId,
            EsValida = esValida,
            PuntajeBase = puntajeBase
        }, cancelacion);

    public Task PublicarParticipanteUnidoSesionAsync(
        Guid sesionId, Guid participanteSesionId, Guid participanteIdentidadId,
        Guid? equipoId, CancellationToken cancelacion)
    {
        var eventoId = Guid.NewGuid();
        return EncolarAsync(eventoId, RoutingKeyParticipante, new
        {
            EventoId = eventoId,
            SesionId = sesionId,
            ParticipanteSesionId = participanteSesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId
        }, cancelacion);
    }

    public Task PublicarEquipoCreadoSesionAsync(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion)
    {
        var eventoId = Guid.NewGuid();
        return EncolarAsync(eventoId, RoutingKeyEquipo, new
        {
            EventoId = eventoId,
            SesionId = sesionId,
            EquipoId = equipoId
        }, cancelacion);
    }

    private async Task EncolarAsync(
        Guid eventoId,
        string routingKey,
        object payload,
        CancellationToken cancelacion)
    {
        _contexto.OutboxRanking.Add(new OutboxMensajeRankingModelo
        {
            Id = eventoId,
            RoutingKey = routingKey,
            PayloadJson = JsonSerializer.Serialize(payload),
            CreadoEnUtc = DateTime.UtcNow,
            Estado = "Pendiente"
        });

        await _contexto.SaveChangesAsync(cancelacion);
    }
}
