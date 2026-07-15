using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerProgresoTrivia;

public sealed record ObtenerProgresoTriviaConsulta(Guid SesionId)
    : IRequest<IReadOnlyList<ProgresoTriviaParticipanteDto>>;
