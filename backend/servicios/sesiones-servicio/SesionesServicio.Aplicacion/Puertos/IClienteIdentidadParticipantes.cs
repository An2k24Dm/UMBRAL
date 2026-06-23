namespace SesionesServicio.Aplicacion.Puertos;

// HU43 — Puerto para resolver datos básicos (no sensibles) de participantes
// contra identidad-servicio, a partir de sus ParticipanteIdentidadId.
public interface IClienteIdentidadParticipantes
{
    Task<IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto>> ObtenerParticipantesPorIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancelacion);
}

// Datos mínimos para mostrar un participante en HU43. Sin correo, teléfono,
// dirección ni fecha de nacimiento.
public sealed class ParticipanteIdentidadResumenDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
}
