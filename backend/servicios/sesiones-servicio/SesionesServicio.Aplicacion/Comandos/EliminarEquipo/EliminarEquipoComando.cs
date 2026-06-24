using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.EliminarEquipo;

// SesionId y EquipoId viajan en la ruta; el líder se resuelve del usuario
// autenticado, no del body. No devuelve cuerpo (204 NoContent).
public sealed record EliminarEquipoComando(
    Guid SesionId,
    Guid EquipoId) : IRequest;
