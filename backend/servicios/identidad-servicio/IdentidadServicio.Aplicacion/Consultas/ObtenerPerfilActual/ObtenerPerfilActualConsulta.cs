using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.Consultas.ObtenerPerfilActual;

public sealed record ObtenerPerfilActualConsulta(string IdKeycloak)
    : IRequest<PerfilUsuarioDto>;
