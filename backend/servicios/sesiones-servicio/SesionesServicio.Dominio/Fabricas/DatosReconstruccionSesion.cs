using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Dominio.Fabricas;

// Datos para reconstruir una sesión como otro tipo (Individual <-> Grupal)
// conservando su identidad. Se usa solo cuando la sesión no tiene
// participantes ni equipos. Preserva Id, código de acceso, estado, fechas y
// operador creador; el modo y la capacidad pueden cambiar.
public sealed record DatosReconstruccionSesion(
    string Modo,
    Guid Id,
    string Nombre,
    string Descripcion,
    DateTime FechaProgramada,
    string CodigoAcceso,
    EstadoSesion Estado,
    Guid OperadorCreadorId,
    DateTime FechaCreacionUtc,
    DateTime? FechaInicioUtc,
    DateTime? FechaFinalizacionUtc,
    IReadOnlyList<Guid> MisionesIds,
    int? MaximoParticipantes,
    int? MaximoEquipos,
    int? MaximoParticipantesPorEquipo,
    int? DuracionMinutosLimite = null);
