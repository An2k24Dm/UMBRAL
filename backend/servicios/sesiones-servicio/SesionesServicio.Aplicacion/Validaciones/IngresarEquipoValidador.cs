using SesionesServicio.Aplicacion.Comandos.IngresarEquipo;

namespace SesionesServicio.Aplicacion.Validaciones;

public sealed class IngresarEquipoValidador : ValidadorBase<IngresarEquipoComando>
{
    protected override void ValidarSolicitud(
        IngresarEquipoComando comando, ResultadoValidacion resultado)
    {
        if (comando.SesionId == Guid.Empty)
            resultado.Agregar("sesionId", "El identificador de la sesión es obligatorio.");

        if (comando.EquipoId == Guid.Empty)
            resultado.Agregar("equipoId", "El identificador del equipo es obligatorio.");

        if (comando.Datos is null)
            resultado.Agregar("solicitud", "El cuerpo de la solicitud es obligatorio.");
    }
}
