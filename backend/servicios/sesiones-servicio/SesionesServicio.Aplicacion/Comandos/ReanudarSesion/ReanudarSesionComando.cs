using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.ReanudarSesion;

public sealed record ReanudarSesionComando(Guid SesionId) : IRequest;
