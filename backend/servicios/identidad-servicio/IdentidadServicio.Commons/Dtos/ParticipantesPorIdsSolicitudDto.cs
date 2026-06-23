namespace IdentidadServicio.Commons.Dtos;

// HU43 — Cuerpo de POST /api/usuarios/participantes/por-ids. Se envían los
// identificadores (Keycloak) de los participantes a resolver.
public sealed class ParticipantesPorIdsSolicitudDto
{
    public IReadOnlyCollection<Guid> ParticipantesIds { get; set; } = Array.Empty<Guid>();
}
