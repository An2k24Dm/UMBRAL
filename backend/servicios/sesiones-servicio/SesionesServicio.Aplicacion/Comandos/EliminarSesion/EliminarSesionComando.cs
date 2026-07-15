using MediatR;

namespace SesionesServicio.Aplicacion.Comandos.EliminarSesion;

public sealed record EliminarSesionComando(Guid Id) : IRequest;
