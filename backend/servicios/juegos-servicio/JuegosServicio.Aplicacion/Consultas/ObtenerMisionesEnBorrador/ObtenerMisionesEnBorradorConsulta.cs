using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerMisionesEnBorrador;

public sealed record ObtenerMisionesEnBorradorConsulta(Guid? CreadorId) : IRequest<List<MisionResumenDto>>;
