using SesionesServicio.Aplicacion.Comandos.IngresarSesionPorCodigo;

namespace SesionesServicio.Aplicacion.Validaciones;

public sealed class ValidadorIngresarSesionPorCodigo
    : ValidadorBase<IngresarSesionPorCodigoComando>
{
    public const int LongitudMaximaCodigo = 32;

    protected override void ValidarSolicitud(
        IngresarSesionPorCodigoComando comando,
        ResultadoValidacion resultado)
    {
        if (comando.Datos is null || string.IsNullOrWhiteSpace(comando.Datos.CodigoSesion))
        {
            resultado.Agregar("codigoSesion", "El código de la sesión es obligatorio.");
            return;
        }

        if (comando.Datos.CodigoSesion.Trim().Length > LongitudMaximaCodigo)
            resultado.Agregar(
                "codigoSesion",
                $"El código de la sesión no puede superar {LongitudMaximaCodigo} caracteres.");
    }
}
