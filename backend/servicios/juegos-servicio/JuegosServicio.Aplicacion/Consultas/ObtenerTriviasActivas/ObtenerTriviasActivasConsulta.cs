using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerTriviasActivas;

public sealed record ObtenerTriviasActivasConsulta() : IRequest<List<TriviaResumenDto>>;
