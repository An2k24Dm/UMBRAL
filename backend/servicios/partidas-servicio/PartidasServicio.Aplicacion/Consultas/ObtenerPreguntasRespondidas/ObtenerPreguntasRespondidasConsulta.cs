using MediatR;

namespace PartidasServicio.Aplicacion.Consultas.ObtenerPreguntasRespondidas;

public sealed record ObtenerPreguntasRespondidasConsulta(
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId)
    : IRequest<IReadOnlyList<Guid>>;
