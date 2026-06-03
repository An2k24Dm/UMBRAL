using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerDetalleMisionConsulta(Guid MisionId) : IRequest<MisionDetalleDto?>;
