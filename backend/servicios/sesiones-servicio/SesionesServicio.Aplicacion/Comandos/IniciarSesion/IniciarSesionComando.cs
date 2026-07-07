using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.IniciarSesion;

public sealed record IniciarSesionComando(Guid SesionId) : IRequest;
