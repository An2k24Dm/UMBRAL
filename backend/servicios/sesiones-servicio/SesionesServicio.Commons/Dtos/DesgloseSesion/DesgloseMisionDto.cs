namespace SesionesServicio.Commons.Dtos.DesgloseSesion;

public sealed record DesgloseMisionDto(
    Guid MisionId,
    int Orden,
    string Nombre,
    long PuntajeTotal,
    IReadOnlyList<DesgloseEtapaDto> Etapas);
