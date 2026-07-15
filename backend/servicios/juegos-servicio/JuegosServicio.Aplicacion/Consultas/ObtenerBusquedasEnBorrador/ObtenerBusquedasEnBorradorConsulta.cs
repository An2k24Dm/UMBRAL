using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerBusquedasEnBorrador;

public sealed record ObtenerBusquedasEnBorradorConsulta(Guid? CreadorId) : IRequest<List<BusquedaTesoroResumenDto>>;
