using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerDetalleMision;

public sealed record ObtenerDetalleMisionConsulta(Guid MisionId) : IRequest<MisionDetalleDto?>;
