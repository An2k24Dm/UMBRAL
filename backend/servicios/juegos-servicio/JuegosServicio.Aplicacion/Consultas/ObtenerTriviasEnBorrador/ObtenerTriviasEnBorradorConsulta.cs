using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerTriviasEnBorrador;

public sealed record ObtenerTriviasEnBorradorConsulta(Guid? OperadorId)
    : IRequest<List<TriviaResumenDto>>;
