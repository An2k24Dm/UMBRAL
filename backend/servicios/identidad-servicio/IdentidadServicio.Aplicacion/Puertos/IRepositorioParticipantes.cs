using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Puertos;

public interface IRepositorioParticipantes
{
    Task AgregarAsync(
        Participante participante, string idKeycloak, CancellationToken cancelacion);
    Task<IReadOnlyList<Participante>> ConsultarAsync(
        int pagina, int tamanioPagina, string? ordenEstado, CancellationToken cancelacion);
    Task<int> ContarAsync(CancellationToken cancelacion);
    Task<Participante?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);
    Task<Participante?> ObtenerPorIdKeycloakAsync(
        string idKeycloak, CancellationToken cancelacion);
    Task<string> ActualizarAsync(Participante participante, CancellationToken cancelacion);
    Task EliminarAsync(Participante participante, CancellationToken cancelacion);
    Task ActualizarEstadoAsync(Participante participante, CancellationToken cancelacion);
}
