using SesionesServicio.Aplicacion.Validaciones;

namespace SesionesServicio.Aplicacion.Comandos.IngresarEquipo;

// HU47 — Validación de formato rápida. La contraseña puede venir null porque
// los equipos públicos no la requieren; si el equipo es privado, el manejador
// la exige y la verifica (necesita consultar el equipo para saber su tipo).
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
