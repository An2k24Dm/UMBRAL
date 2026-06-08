namespace IdentidadServicio.Commons.Dtos;

public sealed class ModificarParticipanteRespuestaDto
{
    public bool HuboCambios { get; set; }
    public IReadOnlyList<string> CamposActualizados { get; set; } = Array.Empty<string>();
    public string Mensaje { get; set; } = string.Empty;
    public PerfilParticipanteDto Participante { get; set; } = new();
}
