using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Validaciones.OperacionSesion;

// Regla EXTRA de negocio para cancelar: una sesión Programada no se cancela,
// se elimina (HU39). El resto de transiciones inválidas (Finalizada,
// Cancelada) las decide el patrón State del dominio.
public sealed class ValidadorCancelacionSesionOperacion
{
    public void Validar(Sesion sesion)
    {
        if (sesion.Estado == EstadoSesion.Programada)
        {
            throw new OperacionSesionInvalidaExcepcion(
                "Una sesión programada no se cancela; debe eliminarse.");
        }
    }
}
