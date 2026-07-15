using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarEtapa;

public sealed record EliminarEtapaComando(Guid MisionId, Guid EtapaId) : IRequest;
