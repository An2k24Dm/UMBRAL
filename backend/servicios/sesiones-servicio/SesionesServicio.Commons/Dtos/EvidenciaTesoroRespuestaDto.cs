namespace SesionesServicio.Commons.Dtos;

public sealed class EvidenciaTesoroRespuestaDto
{
    public bool EsValida { get; set; }
    public Guid EventoId { get; set; }
    public bool EtapaCompletada { get; set; }
}
