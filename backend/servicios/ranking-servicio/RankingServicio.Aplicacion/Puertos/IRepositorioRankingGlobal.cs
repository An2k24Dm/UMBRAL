using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Puertos;

public interface IRepositorioRankingGlobal
{
    Task<RankingGlobalParticipante?> ObtenerPorParticipanteAsync(
        Guid participanteIdentidadId, CancellationToken cancelacion);
    Task<List<RankingGlobalParticipante>> ObtenerTopAsync(
        int cantidad, CancellationToken cancelacion);
    Task AgregarAsync(RankingGlobalParticipante entrada, CancellationToken cancelacion);
    Task ActualizarAsync(RankingGlobalParticipante entrada, CancellationToken cancelacion);
}
