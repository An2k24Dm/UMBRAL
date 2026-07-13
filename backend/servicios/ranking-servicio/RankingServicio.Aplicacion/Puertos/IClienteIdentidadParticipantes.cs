namespace RankingServicio.Aplicacion.Puertos;

// Cliente hacia identidad-servicio para enriquecer, solo al consultar, el alias
// de los participantes a partir de su identificador. Ranking no almacena alias
// ni nombres: los obtiene por id cuando realmente se necesitan para presentar.
public interface IClienteIdentidadParticipantes
{
    Task<IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto>> ObtenerParticipantesPorIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancelacion);
}

public sealed class ParticipanteIdentidadResumenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
}
