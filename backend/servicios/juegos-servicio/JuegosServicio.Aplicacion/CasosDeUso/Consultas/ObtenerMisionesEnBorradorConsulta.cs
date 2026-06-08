using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerMisionesEnBorradorConsulta(Guid? CreadorId) : IRequest<List<MisionResumenDto>>;
