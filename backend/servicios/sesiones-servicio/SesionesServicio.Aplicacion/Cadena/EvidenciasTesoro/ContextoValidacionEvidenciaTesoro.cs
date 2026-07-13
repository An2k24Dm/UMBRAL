using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;

public sealed class ContextoValidacionEvidenciaTesoro
{
    public required Guid SesionId { get; init; }
    public required Guid ParticipanteIdentidadId { get; init; }
    public required Guid MisionId { get; init; }
    public required Guid EtapaId { get; init; }
    public required Guid BusquedaId { get; init; }
    public required string CodigoEscaneado { get; init; }
    public Sesion? Sesion { get; set; }
    public Participante? Participante { get; set; }
    public Guid? EquipoId { get; set; }
    public int TotalCompetidores { get; set; }
    public bool EsCodigoQrValido { get; set; }
}
