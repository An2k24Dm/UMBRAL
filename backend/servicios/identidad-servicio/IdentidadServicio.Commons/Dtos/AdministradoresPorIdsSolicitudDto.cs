namespace IdentidadServicio.Commons.Dtos;

// HU34 — Cuerpo de la solicitud al endpoint POST /api/usuarios/internos/administradores-por-ids.
// Se envía la colección completa por POST (y no por query string) para
// soportar peticiones con muchos identificadores sin chocar con el
// límite de longitud de URL.
public sealed class AdministradoresPorIdsSolicitudDto
{
    public IReadOnlyCollection<Guid> UsuariosIds { get; set; } = Array.Empty<Guid>();
}
