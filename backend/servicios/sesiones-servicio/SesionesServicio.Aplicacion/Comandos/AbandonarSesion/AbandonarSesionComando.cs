using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.AbandonarSesion;

// HU48 — El participante se resuelve del usuario autenticado, no del body.
// El mismo comando sirve para abandonar una sesión individual o el equipo de
// una sesión grupal: el manejador decide según el tipo de sesión. No se
// recibe equipoId porque el participante solo puede estar en un equipo de la
// sesión. No devuelve cuerpo (204 NoContent).
public sealed record AbandonarSesionComando(Guid SesionId) : IRequest;
