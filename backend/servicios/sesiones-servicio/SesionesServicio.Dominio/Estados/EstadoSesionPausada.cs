using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

internal sealed class EstadoSesionPausada : IEstadoSesion
{
    public EstadoSesion Estado => EstadoSesion.Pausada;

    public EstadoSesion Reanudar() => EstadoSesion.Activa;
    public EstadoSesion Finalizar() => EstadoSesion.Finalizada;
    public EstadoSesion Cancelar() => EstadoSesion.Cancelada;

    public EstadoSesion Preparar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Preparar));
    public EstadoSesion Iniciar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Iniciar));
    public EstadoSesion Pausar() => throw new TransicionEstadoSesionInvalidaExcepcion(Estado, nameof(Pausar));
}
