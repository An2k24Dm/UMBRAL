using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

internal sealed class EstadoSesionEnPreparacion : IEstadoSesion
{
    public EstadoSesion Estado => EstadoSesion.EnPreparacion;

    public EstadoSesion Iniciar() => EstadoSesion.Activa;
    public EstadoSesion Cancelar() => EstadoSesion.Cancelada;

    public EstadoSesion Preparar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Preparar));
    public EstadoSesion Pausar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Pausar));
    public EstadoSesion Reanudar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Reanudar));
    public EstadoSesion Finalizar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Finalizar));
}
