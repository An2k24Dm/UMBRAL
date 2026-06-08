using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Dominio.Abstract;

// Contrato del patrón State para las transiciones de una Sesion.
//
// Cada ConcreteState recibe la Sesion (el Context) en cada operación.
// Si la transición es válida, el estado llama a Sesion.CambiarEstado
// con el siguiente ConcreteState; si no, lanza
// TransicionEstadoSesionInvalidaExcepcion. De esta forma, la lógica de
// transición vive en los estados concretos, no en switches dentro de
// Sesion.
//
// El enum EstadoSesion es lo único que se persiste; la fábrica
// FabricaEstadoSesion reconstruye el ConcreteState al rehidratar.
public interface IEstadoSesion
{
    EstadoSesion Estado { get; }

    void Preparar(Sesion sesion);
    void Iniciar(Sesion sesion);
    void Pausar(Sesion sesion);
    void Reanudar(Sesion sesion);
    void Finalizar(Sesion sesion);
    void Cancelar(Sesion sesion);
}
