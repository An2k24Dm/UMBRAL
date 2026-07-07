using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.CancelarSesion;

public sealed record CancelarSesionComando(Guid SesionId) : IRequest;
