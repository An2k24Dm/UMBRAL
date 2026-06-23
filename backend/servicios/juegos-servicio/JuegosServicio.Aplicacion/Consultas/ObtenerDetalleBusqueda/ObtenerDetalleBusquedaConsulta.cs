using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerDetalleBusqueda;

public sealed record ObtenerDetalleBusquedaConsulta(Guid BusquedaId) : IRequest<BusquedaTesoroDetalleDto?>;
