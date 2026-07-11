using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerTriviaParticipante;

public sealed record ObtenerTriviaParticipanteConsulta(
    Guid SesionId,
    Guid MisionId,
    Guid EtapaId,
    Guid TriviaId) : IRequest<TriviaParticipanteJuegosDto?>;
