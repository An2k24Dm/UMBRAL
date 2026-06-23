using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Comandos.ModificarEquipo;

// SesionId y EquipoId viajan en la ruta; el líder se resuelve del usuario
// autenticado, no del body.
public sealed record ModificarEquipoComando(
    Guid SesionId,
    Guid EquipoId,
    ModificarEquipoDto Datos) : IRequest<ModificarEquipoRespuestaDto>;
