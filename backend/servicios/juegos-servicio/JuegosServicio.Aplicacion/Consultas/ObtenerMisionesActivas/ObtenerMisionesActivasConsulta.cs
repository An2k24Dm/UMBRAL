using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerMisionesActivas;

public sealed record ObtenerMisionesActivasConsulta : IRequest<List<MisionResumenDto>>;
