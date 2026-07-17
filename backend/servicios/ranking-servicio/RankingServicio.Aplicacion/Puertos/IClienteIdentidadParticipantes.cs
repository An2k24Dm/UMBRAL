using RankingServicio.Commons.Dtos.ServiciosExternos;

namespace RankingServicio.Aplicacion.Puertos;

public interface IClienteIdentidadParticipantes
{
    Task<IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto>> ObtenerParticipantesPorIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancelacion);
}
