using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

// Estado terminal: ninguna transición es válida.
internal sealed class EstadoSesionFinalizada : IEstadoSesion
{
    private const string Mensaje = "Una sesión Finalizada no permite cambios de estado.";

    public EstadoSesion Estado => EstadoSesion.Finalizada;

    public void Preparar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Preparar), Mensaje);
    public void Iniciar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Iniciar), Mensaje);
    public void Pausar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Pausar), Mensaje);
    public void Reanudar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Reanudar), Mensaje);
    public void Finalizar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Finalizar), Mensaje);
    public void Cancelar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Cancelar), Mensaje);
}
