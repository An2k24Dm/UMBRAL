using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerDetalleEquipoSesion;

// HU43 — Detalle de un equipo (con integrantes) dentro de una sesión.
public sealed record ObtenerDetalleEquipoSesionConsulta(Guid SesionId, Guid EquipoId)
    : IRequest<EquipoSesionDetalleDto>;
