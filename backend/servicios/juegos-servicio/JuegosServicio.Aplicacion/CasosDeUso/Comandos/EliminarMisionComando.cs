using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record EliminarMisionComando(Guid BusquedaId, Guid EtapaId, Guid MisionId) : IRequest;
