using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

// Transiciones permitidas desde Pausada:
//   Reanudar   → Activa
//   Finalizar  → Finalizada
//   Cancelar   → Cancelada
internal sealed class EstadoSesionPausada : IEstadoSesion
{
    public EstadoSesion Estado => EstadoSesion.Pausada;

    public void Reanudar(Sesion sesion)
        => sesion.CambiarEstado(new EstadoSesionActiva());

    public void Finalizar(Sesion sesion)
        => sesion.CambiarEstado(new EstadoSesionFinalizada());

    public void Cancelar(Sesion sesion)
        => sesion.CambiarEstado(new EstadoSesionCancelada());

    public void Preparar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Preparar),
        "Una sesión Pausada no puede volver a EnPreparacion.");

    public void Iniciar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Iniciar),
        "Una sesión Pausada debe reanudarse, no iniciarse.");

    public void Pausar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Pausar),
        "La sesión ya se encuentra Pausada.");
}
