using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Dominio.Excepciones;

// Excepción de regla de negocio: se intenta desactivar/archivar un
// contenido (Trivia o Búsqueda del Tesoro) que aún tiene sesiones
// vigentes (Programada, EnPreparacion, Activa o Pausada) en
// sesiones-servicio. Hereda de ExcepcionDominio para reutilizar el
// mapeo a HTTP 422 (REGLA_NEGOCIO) del middleware existente.
public sealed class ContenidoConSesionesVigentesExcepcion : ExcepcionDominio
{
    public TipoJuego TipoJuego { get; }
    public Guid ContenidoJuegoId { get; }

    public ContenidoConSesionesVigentesExcepcion(TipoJuego tipoJuego, Guid contenidoJuegoId)
        : base("No se puede desactivar este contenido porque tiene sesiones programadas o en ejecución.")
    {
        TipoJuego = tipoJuego;
        ContenidoJuegoId = contenidoJuegoId;
    }
}
