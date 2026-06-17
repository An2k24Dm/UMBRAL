using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

// Transiciones permitidas desde EnPreparacion:
//   Iniciar   → Activa
//   Cancelar  → Cancelada
internal sealed class EstadoSesionEnPreparacion : IEstadoSesion
{
    public EstadoSesion Estado => EstadoSesion.EnPreparacion;

    public void Iniciar(Sesion sesion)
        => sesion.CambiarEstado(new EstadoSesionActiva());

    public void Cancelar(Sesion sesion)
        => sesion.CambiarEstado(new EstadoSesionCancelada());

    public void Preparar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Preparar));

    public void Pausar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Pausar));

    public void Reanudar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Reanudar));

    public void Finalizar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Finalizar));
}
