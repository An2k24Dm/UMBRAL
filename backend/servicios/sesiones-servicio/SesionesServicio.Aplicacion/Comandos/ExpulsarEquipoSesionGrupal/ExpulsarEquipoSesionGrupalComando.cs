using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.ExpulsarEquipoSesionGrupal;

// HU44 — SesionId y EquipoId viajan en la ruta; el operador se resuelve del
// usuario autenticado, no del body. No devuelve cuerpo (204 NoContent).
public sealed record ExpulsarEquipoSesionGrupalComando(
    Guid SesionId,
    Guid EquipoId) : IRequest;
