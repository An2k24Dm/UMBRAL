namespace IdentidadServicio.Commons.Dtos;

// HU34 — Respuesta a la consulta de "¿cuáles de estos usuarios son
// Administradores?" usada por sesiones-servicio para resolver la
// regla de visibilidad de sesiones sin guardar el rol del creador.
//
// Devuelve únicamente los identificadores que corresponden a usuarios
// internos con rol Administrador. Cualquier id desconocido o que
// pertenezca a un Operador/Participante se omite del resultado.
public sealed class AdministradoresPorIdsRespuestaDto
{
    public IReadOnlyCollection<Guid> AdministradoresIds { get; set; } = Array.Empty<Guid>();
}
