using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerSesionPorId;

public sealed record ObtenerSesionPorIdConsulta(Guid Id) : IRequest<SesionDetalleDto?>;
