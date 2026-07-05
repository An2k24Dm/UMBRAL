using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.IngresarEquipo;

public sealed record IngresarEquipoComando(
    Guid SesionId,
    Guid EquipoId,
    IngresarEquipoDto Datos) : IRequest<IngresarEquipoRespuestaDto>;
