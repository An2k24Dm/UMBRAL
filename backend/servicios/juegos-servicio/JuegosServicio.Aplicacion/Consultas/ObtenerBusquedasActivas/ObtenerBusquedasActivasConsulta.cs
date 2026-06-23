using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerBusquedasActivas;

public sealed record ObtenerBusquedasActivasConsulta : IRequest<List<BusquedaTesoroResumenDto>>;
