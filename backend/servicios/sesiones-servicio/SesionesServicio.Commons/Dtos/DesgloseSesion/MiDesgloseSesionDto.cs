namespace SesionesServicio.Commons.Dtos.DesgloseSesion;

public sealed record MiDesgloseSesionDto(
    Guid ParticipanteIdentidadId,
    long PuntajeBruto,
    long PuntosPenalizados,
    long PuntajeTotal,
    IReadOnlyList<DesgloseMisionDto> Misiones);
