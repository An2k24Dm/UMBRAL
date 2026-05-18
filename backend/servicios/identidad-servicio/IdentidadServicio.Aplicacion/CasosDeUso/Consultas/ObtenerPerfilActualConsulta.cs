using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Consultas;

public sealed record ObtenerPerfilActualConsulta(string IdKeycloak)
    : IRequest<PerfilUsuarioDto>;
