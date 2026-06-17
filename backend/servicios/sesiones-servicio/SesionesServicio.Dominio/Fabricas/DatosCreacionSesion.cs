namespace SesionesServicio.Dominio.Fabricas;

public sealed record DatosCreacionSesion(
    string Modo,
    string Nombre,
    string Descripcion,
    DateTime FechaProgramada,
    string CodigoAcceso,
    Guid OperadorCreadorId,
    DateTime FechaCreacionUtc,
    int? MaximoParticipantes,
    int? MaximoEquipos,
    int? MaximoParticipantesPorEquipo);
