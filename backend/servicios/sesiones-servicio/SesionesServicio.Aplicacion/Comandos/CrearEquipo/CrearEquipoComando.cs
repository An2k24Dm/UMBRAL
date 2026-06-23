using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.CrearEquipo;

// SesionId viaja en la ruta; el líder se resuelve del usuario autenticado.
public sealed record CrearEquipoComando(
    Guid SesionId,
    CrearEquipoDto Datos) : IRequest<CrearEquipoRespuestaDto>;
