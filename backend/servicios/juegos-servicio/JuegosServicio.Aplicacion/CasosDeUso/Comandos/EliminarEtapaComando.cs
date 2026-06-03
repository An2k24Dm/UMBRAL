using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Comandos;

public sealed record EliminarEtapaComando(Guid MisionId, Guid EtapaId) : IRequest;
