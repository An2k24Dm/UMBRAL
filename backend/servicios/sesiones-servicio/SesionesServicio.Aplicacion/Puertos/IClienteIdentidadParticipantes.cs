using SesionesServicio.Commons.Dtos.ServiciosExternos;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IClienteIdentidadParticipantes
{
    Task<IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto>> ObtenerParticipantesPorIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancelacion);
}
