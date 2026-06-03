using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record ActivarMisionComando(Guid MisionId) : IRequest;
