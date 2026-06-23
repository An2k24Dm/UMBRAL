using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerDetalleTrivia;

public sealed record ObtenerDetalleTriviaConsulta(Guid TriviaId) : IRequest<TriviaDetalleDto?>;
