namespace IdentidadServicio.Commons.Dtos;

public sealed class EliminarCuentaParticipanteRespuestaDto
{
    public bool Eliminada { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}
