using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.IngresarEquipo;

// HU47 — SesionId y EquipoId viajan en la ruta; el participante se resuelve
// del usuario autenticado, no del body.
public sealed record IngresarEquipoComando(
    Guid SesionId,
    Guid EquipoId,
    IngresarEquipoDto Datos) : IRequest<IngresarEquipoRespuestaDto>;
