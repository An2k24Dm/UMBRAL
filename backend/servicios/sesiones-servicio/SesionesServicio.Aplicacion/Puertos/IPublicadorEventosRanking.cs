namespace SesionesServicio.Aplicacion.Puertos;

// Publica hacia ranking-servicio solo los identificadores estrictamente
// necesarios para construir su dominio. Ranking no almacena nombres ni alias:
// los enriquece por id al consultar. ParticipanteSesionId identifica la
// participación concreta (Participante.Id); ParticipanteIdentidadId identifica
// al usuario para agrupar en el ranking global.
public interface IPublicadorEventosRanking
{
    Task PublicarRespuestaTriviaRegistradaAsync(
        Guid sesionId,
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        int puntaje,
        CancellationToken cancelacion);

    Task PublicarEvidenciaTesoroRegistradaAsync(
        Guid sesionId,
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        int puntaje,
        CancellationToken cancelacion);

    Task PublicarParticipanteUnidoSesionAsync(
        Guid sesionId,
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        CancellationToken cancelacion);

    Task PublicarEquipoCreadoSesionAsync(
        Guid sesionId,
        Guid equipoId,
        CancellationToken cancelacion);
}
