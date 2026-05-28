using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerTriviasActivasConsulta() : IRequest<List<TriviaResumenDto>>;
