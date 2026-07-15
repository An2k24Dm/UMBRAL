namespace SesionesServicio.Aplicacion.Puertos;

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
