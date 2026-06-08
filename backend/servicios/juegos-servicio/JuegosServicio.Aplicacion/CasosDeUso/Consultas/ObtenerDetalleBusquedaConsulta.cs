using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerDetalleBusquedaConsulta(Guid BusquedaId) : IRequest<BusquedaTesoroDetalleDto?>;
