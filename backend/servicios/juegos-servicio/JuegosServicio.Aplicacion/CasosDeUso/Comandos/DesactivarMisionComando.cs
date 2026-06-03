using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record DesactivarMisionComando(Guid MisionId) : IRequest;
