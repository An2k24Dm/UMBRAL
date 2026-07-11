namespace SesionesServicio.Infraestructura.Persistencia;

public sealed class EvidenciaTesoroModelo
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public Guid MisionId { get; set; }
    public Guid EtapaId { get; set; }
    public Guid BusquedaId { get; set; }
    public Guid ParticipanteIdentidadId { get; set; }
    // Null en sesión individual; id del equipo del integrante en sesión grupal.
    public Guid? EquipoId { get; set; }
    public string CodigoEnviado { get; set; } = string.Empty;
    public bool EsValida { get; set; }
    public int PuntosGanados { get; set; }
    public DateTime FechaEnvioUtc { get; set; }
}

public sealed class PistaLiberadaModelo
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public Guid EtapaId { get; set; }
    public Guid? PistaId { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public DateTime FechaLiberacionUtc { get; set; }
}
