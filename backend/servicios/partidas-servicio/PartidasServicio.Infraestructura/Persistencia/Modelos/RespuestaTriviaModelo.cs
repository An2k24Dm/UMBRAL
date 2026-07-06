namespace PartidasServicio.Infraestructura.Persistencia.Modelos;

public sealed class RespuestaTriviaModelo
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public Guid MisionId { get; set; }
    public Guid EtapaId { get; set; }
    public Guid PreguntaId { get; set; }
    public Guid OpcionSeleccionadaId { get; set; }
    public Guid ParticipanteId { get; set; }
    public Guid? EquipoId { get; set; }
    public bool EsCorrecta { get; set; }
    public int PuntosGanados { get; set; }
    public long TiempoTardadoMs { get; set; }
    public DateTime FechaRespuestaUtc { get; set; }
}
