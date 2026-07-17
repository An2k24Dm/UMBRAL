using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Commons.Dtos.Sesiones;

public sealed record SesionParticipacionActivaDto(
    Guid SesionId,
    string NombreSesion,
    EstadoSesion Estado,
    ModoSesion Modo,
    Guid? EquipoId,
    string? EquipoNombre);
