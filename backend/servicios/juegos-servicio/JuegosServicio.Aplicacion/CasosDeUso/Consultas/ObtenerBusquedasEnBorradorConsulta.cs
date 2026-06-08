using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerBusquedasEnBorradorConsulta(Guid? CreadorId) : IRequest<List<BusquedaTesoroResumenDto>>;
