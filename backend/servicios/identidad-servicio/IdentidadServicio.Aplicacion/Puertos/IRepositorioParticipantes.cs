using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Puertos;

// Puerto específico para Participantes. Lo usan HU03 (registro público) y
// HU07 (listado/detalle desde el panel web).
public interface IRepositorioParticipantes
{
    // HU03 — alta. La confirmación se hace por IUnidadTrabajoIdentidad.
    Task AgregarAsync(
        Participante participante, string idKeycloak, CancellationToken cancelacion);

    // HU07 — listado paginado. Filtra estrictamente por rol Participante.
    Task<IReadOnlyList<Participante>> ConsultarAsync(
        int pagina, int tamanioPagina, string? ordenEstado, CancellationToken cancelacion);

    Task<int> ContarAsync(CancellationToken cancelacion);

    // HU07 — detalle por id. Devuelve null si el id no existe o no es
    // Participante.
    Task<Participante?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);

    // HU10 — el Participante autenticado edita su propio perfil. El backend
    // identifica al usuario por el sub del token (IdKeycloak)
    Task<Participante?> ObtenerPorIdKeycloakAsync(
        string idKeycloak, CancellationToken cancelacion);

    // HU10 — edición parcial. Aplica los campos editables al modelo EF (sin
    // SaveChanges; la confirmación va por IUnidadTrabajoIdentidad). Devuelve
    // el IdKeycloak del Participante para que el manejador sincronice
    // Keycloak posteriormente.
    Task<string> ActualizarAsync(Participante participante, CancellationToken cancelacion);
}
