using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerDetalleTriviaConsulta(Guid TriviaId) : IRequest<TriviaDetalleDto?>;
