using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerTriviasEnBorradorConsulta(Guid OperadorId)
    : IRequest<List<TriviaResumenDto>>;
