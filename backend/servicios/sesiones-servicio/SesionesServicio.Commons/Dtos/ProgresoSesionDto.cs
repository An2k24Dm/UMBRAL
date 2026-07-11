namespace SesionesServicio.Commons.Dtos;

public sealed class ProgresoSesionParticipanteDto
{
    public Guid ParticipanteIdentidadId { get; set; }
    public Guid? EquipoId { get; set; }
    // Trivia
    public int TriviaRespondidas { get; set; }
    public int TriviaCorrectas { get; set; }
    public int TriviaIncorrectas { get; set; }
    public int TriviaPuntosGanados { get; set; }
    // Búsqueda del tesoro
    public int TesoroIntentosEnviados { get; set; }
    public int TesoroEtapasCompletadas { get; set; }
    public int TesoroPuntosGanados { get; set; }
    // Total
    public int TotalPuntosGanados { get; set; }
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
