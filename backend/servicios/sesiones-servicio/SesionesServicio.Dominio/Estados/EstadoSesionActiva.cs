using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Estados;

// Transiciones permitidas desde Activa:
//   Pausar     → Pausada
//   Finalizar  → Finalizada
//   Cancelar   → Cancelada
internal sealed class EstadoSesionActiva : IEstadoSesion
{
    public EstadoSesion Estado => EstadoSesion.Activa;

    public void Pausar(Sesion sesion)
        => sesion.CambiarEstado(new EstadoSesionPausada());

    public void Finalizar(Sesion sesion)
        => sesion.CambiarEstado(new EstadoSesionFinalizada());

    public void Cancelar(Sesion sesion)
        => sesion.CambiarEstado(new EstadoSesionCancelada());

    public void Preparar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Preparar),
        "Una sesión Activa no puede volver a EnPreparacion.");

    public void Iniciar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Iniciar),
        "La sesión ya se encuentra Activa.");

    public void Reanudar(Sesion sesion) => throw new TransicionEstadoSesionInvalidaExcepcion(
        Estado, nameof(Reanudar),
        "La sesión ya se encuentra Activa.");
}
