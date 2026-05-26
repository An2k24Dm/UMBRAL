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
}
