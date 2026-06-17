using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

// Estado terminal: ninguna transición es válida.
internal sealed class EstadoSesionFinalizada : IEstadoSesion
{
    public EstadoSesion Estado => EstadoSesion.Finalizada;

    public void Preparar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Preparar));
    public void Iniciar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Iniciar));
    public void Pausar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Pausar));
    public void Reanudar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Reanudar));
    public void Finalizar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Finalizar));
    public void Cancelar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Cancelar));
}
