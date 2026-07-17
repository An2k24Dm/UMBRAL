namespace SesionesServicio.Commons.Dtos.DesgloseSesion;

public sealed record DesgloseEtapaDto(
    Guid EtapaId,
    int Orden,
    string Nombre,
    string Tipo,
    long Puntaje);
