using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Aplicacion.Puertos;

// Puerto saliente hacia sesiones-servicio. Lo usan los manejadores
// que desactivan/archivar contenido (Trivia, Búsqueda del Tesoro)
// para confirmar que no quede ninguna sesión vigente apuntando al
// contenido antes de cambiarle el estado.
//
// La implementación HTTP vive en Infraestructura. La regla de "qué
// estados son vigentes" es responsabilidad de sesiones-servicio; este
// puerto sólo expone el resultado booleano.
public interface IClienteSesiones
{
    Task<bool> ExisteSesionVigentePorContenidoAsync(
        TipoJuego tipoJuego,
        Guid contenidoJuegoId,
        CancellationToken cancelacion);
}
