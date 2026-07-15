using SesionesServicio.Aplicacion.Comandos.AbandonarSesion;

namespace SesionesServicio.Aplicacion.Validaciones;

// HU48 — Validación de formato rápida; las reglas de negocio (estado y
// pertenencia) las protege el dominio.
public sealed class ValidadorAbandonarSesion
    : ValidadorBase<AbandonarSesionComando>
{
    protected override void ValidarSolicitud(
        AbandonarSesionComando comando,
        ResultadoValidacion resultado)
    {
        if (comando.SesionId == Guid.Empty)
            resultado.Agregar(
                "sesionId",
                "El identificador de la sesión es obligatorio.");
    }
}
