using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Dominio.Abstract;

// Contrato del patrón State para las transiciones de una Sesion.
// Cada implementación encapsula las transiciones válidas desde un
// EstadoSesion concreto y devuelve el siguiente valor del enum, que es
// lo único que se persiste. La entidad Sesion delega en este contrato
// para mantener centralizadas las reglas de transición.
public interface IEstadoSesion
{
    EstadoSesion Estado { get; }

    EstadoSesion Preparar();
    EstadoSesion Iniciar();
    EstadoSesion Pausar();
    EstadoSesion Reanudar();
    EstadoSesion Finalizar();
    EstadoSesion Cancelar();
}
