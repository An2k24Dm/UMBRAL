using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Consultas.ConsultarParticipantesPorIds;

// HU43 — Resuelve datos básicos (nombre/apellido/alias) de participantes a
// partir de sus identificadores de Keycloak.
public sealed record ConsultarParticipantesPorIdsConsulta(
    IReadOnlyCollection<Guid> ParticipantesIds)
    : IRequest<IReadOnlyList<ParticipanteBasicoDto>>;
