using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record EliminarEtapaComando(Guid BusquedaId, Guid EtapaId) : IRequest;
