namespace SesionesServicio.Commons.Dtos;

public sealed class EstadoPartidaDto
{
    public string Estado { get; set; } = string.Empty;
    public bool ParticipanteInscrito { get; set; }
    public Guid? EquipoId { get; set; }
}
