using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerTriviaParticipante;

public sealed record ObtenerTriviaParticipanteConsulta(Guid TriviaId)
    : IRequest<TriviaParticipanteDto?>;
