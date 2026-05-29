using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

internal sealed class EstadoSesionProgramada : IEstadoSesion
{
    public EstadoSesion Estado => EstadoSesion.Programada;

    public EstadoSesion Preparar() => EstadoSesion.EnPreparacion;
    public EstadoSesion Cancelar() => EstadoSesion.Cancelada;

    public EstadoSesion Iniciar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Iniciar));
    public EstadoSesion Pausar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Pausar));
    public EstadoSesion Reanudar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Reanudar));
    public EstadoSesion Finalizar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Finalizar));
}
