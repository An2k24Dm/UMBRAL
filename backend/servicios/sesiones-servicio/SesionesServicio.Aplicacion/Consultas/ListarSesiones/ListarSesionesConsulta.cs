using MediatR;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Consultas.ListarSesiones;

public sealed record ListarSesionesConsulta(EstadoSesion? Estado)
    : IRequest<List<SesionListadoDto>>;
