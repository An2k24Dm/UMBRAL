using MediatR;
using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ListarSesionesConsulta() : IRequest<List<SesionListadoDto>>;
