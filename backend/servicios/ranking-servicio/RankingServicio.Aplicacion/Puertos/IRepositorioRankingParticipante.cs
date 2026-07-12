using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Puertos;

public interface IRepositorioRankingParticipante
{
    Task<EntradaRankingParticipante?> ObtenerPorSesionYParticipanteAsync(
        Guid sesionId, Guid participanteIdentidadId, CancellationToken cancelacion);
    Task<List<EntradaRankingParticipante>> ObtenerPorSesionAsync(
        Guid sesionId, CancellationToken cancelacion);
    Task AgregarAsync(EntradaRankingParticipante entrada, CancellationToken cancelacion);
    Task ActualizarAsync(EntradaRankingParticipante entrada, CancellationToken cancelacion);
}
