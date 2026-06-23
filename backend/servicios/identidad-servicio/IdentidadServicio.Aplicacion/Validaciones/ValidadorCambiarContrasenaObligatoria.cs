using IdentidadServicio.Aplicacion.Comandos.CambiarContrasenaObligatoria;
using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Aplicacion.Validaciones;

public sealed class ValidadorCambiarContrasenaObligatoria
    : ValidadorBase<CambiarContrasenaObligatoriaComando>
{
    private readonly IReglasValidacionUsuario _reglas;

    public ValidadorCambiarContrasenaObligatoria(IReglasValidacionUsuario reglas)
    {
        _reglas = reglas;
    }

    protected override void ValidarSolicitud(
        CambiarContrasenaObligatoriaComando comando, ResultadoValidacion resultado)
    {
        var dto = comando.Datos;

        _reglas.ValidarContrasena(dto.NuevaContrasena, resultado);

        if (!string.Equals(dto.NuevaContrasena, dto.ConfirmacionContrasena, StringComparison.Ordinal))
        {
            resultado.Agregar(
                MensajesValidacionUsuario.CampoConfirmacionContrasena,
                MensajesValidacionUsuario.ContrasenasNoCoinciden);
        }
    }
}
