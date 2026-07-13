namespace SesionesServicio.Aplicacion.Puertos;

public interface IPublicadorEventosRanking
{
    Task PublicarRespuestaTriviaRegistradaAsync(
        Guid eventoId,
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        Guid triviaId,
        Guid preguntaId,
        bool esCorrecta,
        int puntajeBase,
        int tiempoTardadoMs,
        int tiempoLimiteMs,
        CancellationToken cancelacion);

    Task PublicarEvidenciaTesoroRegistradaAsync(
        Guid eventoId,
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        Guid busquedaId,
        bool esValida,
        int puntajeBase,
        int ordenResolucion,
        int totalCompetidores,
        int tiempoTranscurridoMs,
        int tiempoLimiteMs,
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
