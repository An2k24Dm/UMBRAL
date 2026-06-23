using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ListarEquiposSesion;

// HU43 — Listado de equipos de una sesión grupal. La identidad del usuario
// actual (para EsMiEquipo/SoyLider) sale del token, no del request.
public sealed record ListarEquiposSesionConsulta(Guid SesionId)
    : IRequest<IReadOnlyList<EquipoSesionListadoDto>>;
