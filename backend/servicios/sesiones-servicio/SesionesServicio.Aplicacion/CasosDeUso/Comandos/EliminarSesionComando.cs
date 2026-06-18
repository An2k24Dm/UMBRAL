using MediatR;

namespace SesionesServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record EliminarSesionComando(Guid Id) : IRequest;
