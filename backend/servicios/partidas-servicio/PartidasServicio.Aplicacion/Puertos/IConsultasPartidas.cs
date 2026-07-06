using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.Aplicacion.Puertos;

public interface IConsultasPartidas
{
    Task<IReadOnlyList<RankingEntradaDto>> ObtenerRankingAsync(Guid sesionId, CancellationToken cancelacion);
}
