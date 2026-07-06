using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Validaciones.OperacionSesion;

// Reglas EXTRA de aplicación para iniciar una sesión que el patrón State no
// cubre: fecha programada aún no cumplida y ausencia de inscritos. La validez
// de la transición de estado (Iniciar) la decide el propio State del dominio.
public sealed class ValidadorInicioSesionOperacion
{
    public void Validar(Sesion sesion, DateTime ahoraUtc)
    {
        if (sesion.Estado == EstadoSesion.Programada &&
            sesion.FechaProgramada > ahoraUtc)
        {
            throw new OperacionSesionInvalidaExcepcion(
                "No se puede iniciar una sesión antes de su fecha programada.");
        }

        if (!sesion.TieneInscritos)
        {
            throw new OperacionSesionInvalidaExcepcion(
                "La sesión no puede iniciar porque no tiene participantes o equipos inscritos.");
        }
    }
}
