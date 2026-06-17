using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

// Transiciones permitidas desde Programada:
//   Preparar  → EnPreparacion
//   Cancelar  → Cancelada
internal sealed class EstadoSesionProgramada : IEstadoSesion
{
    public EstadoSesion Estado => EstadoSesion.Programada;

    public void Preparar(Sesion sesion)
        => sesion.CambiarEstado(new EstadoSesionEnPreparacion());

    public void Cancelar(Sesion sesion)
        => sesion.CambiarEstado(new EstadoSesionCancelada());

    public void Iniciar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Iniciar));

    public void Pausar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Pausar));

    public void Reanudar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Reanudar));

    public void Finalizar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Finalizar));
}
