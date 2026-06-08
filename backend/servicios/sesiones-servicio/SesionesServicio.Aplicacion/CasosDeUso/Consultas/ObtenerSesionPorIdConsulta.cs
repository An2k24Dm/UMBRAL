using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerSesionPorIdConsulta(Guid Id) : IRequest<SesionDetalleDto?>;
