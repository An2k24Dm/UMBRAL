using MediatR;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ListarSesionesConsulta(EstadoSesion? Estado)
    : IRequest<List<SesionListadoDto>>;
