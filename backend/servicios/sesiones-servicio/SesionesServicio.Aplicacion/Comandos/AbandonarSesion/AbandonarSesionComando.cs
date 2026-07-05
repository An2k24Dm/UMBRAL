using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.AbandonarSesion;

public sealed record AbandonarSesionComando(Guid SesionId) : IRequest;
