using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

// Estado terminal: ninguna transición es válida.
internal sealed class EstadoSesionFinalizada : IEstadoSesion
{
    public EstadoSesion Estado => EstadoSesion.Finalizada;

    public EstadoSesion Preparar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Preparar));
    public EstadoSesion Iniciar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Iniciar));
    public EstadoSesion Pausar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Pausar));
    public EstadoSesion Reanudar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Reanudar));
    public EstadoSesion Finalizar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Finalizar));
    public EstadoSesion Cancelar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Cancelar));
}
