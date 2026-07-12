namespace SesionesServicio.Aplicacion.Puertos;

public interface IPublicadorEventosRanking
{
    Task PublicarRespuestaTriviaRegistradaAsync(
        Guid sesionId,
        Guid participanteIdentidadId,
        string nombreParticipante,
        Guid? equipoId,
        string? nombreEquipo,
        int puntaje,
        bool esCorrecta,
        CancellationToken cancelacion);

    Task PublicarEvidenciaTesoroRegistradaAsync(
        Guid sesionId,
        Guid participanteIdentidadId,
        string nombreParticipante,
        Guid? equipoId,
        string? nombreEquipo,
        int puntaje,
        CancellationToken cancelacion);

    Task PublicarSesionFinalizadaAsync(
        Guid sesionId,
        bool esGrupal,
        CancellationToken cancelacion);

    Task PublicarParticipanteUnidoSesionAsync(
        Guid sesionId,
        Guid participanteIdentidadId,
        string nombreParticipante,
        CancellationToken cancelacion);

    Task PublicarEquipoCreadoSesionAsync(
        Guid sesionId,
        Guid equipoId,
        string nombreEquipo,
        CancellationToken cancelacion);
}
