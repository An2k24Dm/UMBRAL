namespace SesionesServicio.Commons.Dtos;

public sealed class ProgresoSecuencialSesionDto
{
    public List<Guid> EtapasCompletadasGlobalmenteIds { get; set; } = new();
    public List<Guid> EtapasCompletadasIds
    {
        get => EtapasCompletadasGlobalmenteIds;
        set => EtapasCompletadasGlobalmenteIds = value;
    }
    public Guid? MisionActualId { get; set; }
    public Guid? EtapaActualId { get; set; }
    public string? TipoEtapaActual { get; set; }
    public Guid? ModoDeJuegoId { get; set; }
    public int? OrdenGlobalActual { get; set; }
    // Preparación entre etapas/misiones. "Preparacion" ⇒ la etapa está
    // identificada pero NO es jugable todavía; el frontend NO debe navegar,
    // solo mostrar el banner con la cuenta regresiva autoritativa.
    public string? FaseEtapaActual { get; set; }
    public DateTime? FechaInicioProgramadaEtapaUtc { get; set; }
    public int? SegundosRestantesPreparacion { get; set; }
    public int? DuracionPreparacionSegundos { get; set; }
    public int? NumeroMisionActual { get; set; }
    public int? NumeroEtapaActual { get; set; }
    public bool EsNuevaMision { get; set; }
    public DateTime? FechaInicioEtapaUtc { get; set; }
    public int? DuracionEtapaSegundos { get; set; }
    public long DuracionPausasAcumuladaMs { get; set; }
    public DateTime? FechaInicioPausaUtc { get; set; }
    public int? SegundosRestantesEtapa { get; set; }
    public long? TiempoActivoEtapaMs { get; set; }
    public Guid? TriviaPreguntaActualId { get; set; }
    public List<Guid> TriviaPreguntasExpiradasIds { get; set; } = new();
    public int? TriviaTiempoRestantePreguntaMs { get; set; }
    public int? TriviaTiempoTranscurridoPreguntaMs { get; set; }
    public bool TriviaAgotada { get; set; }
    // Ventana de feedback autoritativa entre preguntas (mínimo 5 s). Durante ella
    // no hay pregunta respondible; el móvil muestra el resultado y el countdown
    // real (sobrevive a reconexiones porque el backend devuelve el restante).
    public bool TriviaEnTransicionEntrePreguntas { get; set; }
    public int? TriviaTiempoRestanteTransicionMs { get; set; }
    public Guid? TriviaSiguientePreguntaId { get; set; }
    public bool JugadorActualCompletoEtapaActual { get; set; }
    public bool EsperandoOtrosJugadores { get; set; }
    public bool TodoCompletado { get; set; }
}
