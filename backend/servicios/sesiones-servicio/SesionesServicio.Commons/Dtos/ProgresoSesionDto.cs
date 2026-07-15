namespace SesionesServicio.Commons.Dtos;

public sealed class ProgresoSesionDto
{
    public Guid? MisionActualId { get; set; }
    public Guid? EtapaActualId { get; set; }
    public int? OrdenMisionActual { get; set; }
    public int? OrdenEtapaActual { get; set; }
    public string? TipoEtapaActual { get; set; }
    public string? FaseEtapaActual { get; set; }
    public List<ProgresoSesionParticipanteDto> Filas { get; set; } = new();
}

public sealed class ProgresoSesionParticipanteDto
{
    public Guid ParticipanteIdentidadId { get; set; }
    public Guid? EquipoId { get; set; }
    // Trivia
    public int TriviaEtapasCompletadas { get; set; }
    public int TriviaRespondidas { get; set; }
    public int TriviaCorrectas { get; set; }
    public int TriviaIncorrectas { get; set; }
    // Búsqueda del tesoro
    public int TesoroIntentosEnviados { get; set; }
    public int TesoroEtapasCompletadas { get; set; }
}

public sealed class MiParticipacionDto
{
    public Guid SesionId { get; set; }
    public string NombreSesion { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
    public DateTime? FechaInicioUtc { get; set; }
    public DateTime? FechaFinalizacionUtc { get; set; }
    public int PuntajeObtenido { get; set; }
}
