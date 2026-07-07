using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.Aplicacion.Puertos;

public interface IConsultasPartidas
{
    Task<IReadOnlyList<RankingEntradaDto>> ObtenerRankingAsync(Guid sesionId, CancellationToken cancelacion);

    Task<IReadOnlyList<Guid>> ObtenerPreguntasRespondidasAsync(
        Guid sesionId, Guid misionId, Guid etapaId,
        Guid? equipoId, Guid? participanteId,
        CancellationToken cancelacion);
}
