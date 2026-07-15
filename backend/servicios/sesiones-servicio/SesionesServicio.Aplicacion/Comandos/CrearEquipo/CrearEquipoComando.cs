using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.CrearEquipo;

public sealed record CrearEquipoComando(
    Guid SesionId,
    CrearEquipoDto Datos) : IRequest<CrearEquipoRespuestaDto>;
