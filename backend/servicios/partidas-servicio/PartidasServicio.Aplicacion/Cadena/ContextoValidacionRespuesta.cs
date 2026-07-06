namespace PartidasServicio.Aplicacion.Cadena;

public sealed class ContextoValidacionRespuesta
{
    public Guid SesionId { get; init; }
    public Guid PreguntaId { get; init; }
    public Guid ParticipanteId { get; init; }

    // Poblado por EslabonEstadoSesion
    public string EstadoSesion { get; set; } = string.Empty;

    // Poblado por EslabonParticipanteEnSesion
    public bool ParticipanteInscrito { get; set; }
    public Guid? EquipoId { get; set; }

    // Poblado por EslabonConcurrencia
    public bool PreguntaYaRespondida { get; set; }
}
