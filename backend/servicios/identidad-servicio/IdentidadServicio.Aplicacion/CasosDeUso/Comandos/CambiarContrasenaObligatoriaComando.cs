using IdentidadServicio.Commons.Dtos;
using MediatR;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Comandos;

// IdKeycloak viene del JWT (sub) y NO del cuerpo: el usuario no puede
// cambiar la contraseña de otro presentando un JWT propio.
public sealed record CambiarContrasenaObligatoriaComando(
    string IdKeycloak,
    CambiarContrasenaObligatoriaDto Datos)
    : IRequest<CambiarContrasenaObligatoriaRespuestaDto>;
