using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ValidadorAccesoUsuarioActivo : IValidadorAccesoUsuarioActivo
{
    private readonly IRepositorioUsuariosLectura _repositorioLectura;

    public ValidadorAccesoUsuarioActivo(IRepositorioUsuariosLectura repositorioLectura)
    {
        _repositorioLectura = repositorioLectura;
    }

    public async Task<ResultadoAccesoUsuarioActivo> ValidarAsync(
        string idKeycloak, CancellationToken cancelacion)
    {
        var usuario = await _repositorioLectura.ObtenerPorIdKeycloakAsync(
            idKeycloak, cancelacion);

        if (usuario is not null && usuario.Estado != EstadoUsuario.Activo)
            return ResultadoAccesoUsuarioActivo.Bloqueado(
                "CUENTA_DESACTIVADA", "La cuenta se encuentra desactivada.");

        return ResultadoAccesoUsuarioActivo.Permitido();
    }
}
