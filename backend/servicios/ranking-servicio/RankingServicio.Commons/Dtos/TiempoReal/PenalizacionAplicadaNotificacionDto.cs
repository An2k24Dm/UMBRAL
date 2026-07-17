namespace RankingServicio.Commons.Dtos.TiempoReal;

public sealed record PenalizacionAplicadaNotificacionDto(
    Guid SesionId,
    string TipoObjetivo,
    Guid ObjetivoId,
    int PuntosPenalizados,
    int PuntosPenalizadosAcumulados,
    long PuntajeResultante,
    DateTime AplicadaEnUtc);
