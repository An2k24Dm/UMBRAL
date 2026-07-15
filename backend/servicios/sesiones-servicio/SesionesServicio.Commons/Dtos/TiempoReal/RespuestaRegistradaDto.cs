namespace SesionesServicio.Commons.Dtos.TiempoReal;

public sealed class RespuestaRegistradaDto
{
    public Guid SesionId { get; set; }
    public Guid EtapaId { get; set; }
    public Guid PreguntaId { get; set; }
    public Guid ParticipanteIdentidadId { get; set; }
    public Guid? EquipoId { get; set; }
    public bool EsCorrecta { get; set; }
    public DateTime FechaEventoUtc { get; set; }
}
