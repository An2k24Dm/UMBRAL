using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Consultas;

// HU34 — Consulta usada por sesiones-servicio para resolver "¿cuáles
// de estos usuarios son Administradores?" sin exponer información
// sensible (sólo devuelve identificadores).
public sealed record FiltrarAdministradoresPorIdsConsulta(
    IReadOnlyCollection<Guid> UsuariosIds)
    : IRequest<AdministradoresPorIdsRespuestaDto>;
