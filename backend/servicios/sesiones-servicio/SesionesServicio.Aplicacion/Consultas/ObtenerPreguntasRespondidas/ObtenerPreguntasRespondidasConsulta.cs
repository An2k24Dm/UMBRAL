using MediatR;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerPreguntasRespondidas;

public sealed record ObtenerPreguntasRespondidasConsulta(
    Guid SesionId,
    Guid EtapaId)
    : IRequest<IReadOnlyList<Guid>>;
