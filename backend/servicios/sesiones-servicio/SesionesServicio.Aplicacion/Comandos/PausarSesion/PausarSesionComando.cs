using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.PausarSesion;

public sealed record PausarSesionComando(Guid SesionId) : IRequest;
