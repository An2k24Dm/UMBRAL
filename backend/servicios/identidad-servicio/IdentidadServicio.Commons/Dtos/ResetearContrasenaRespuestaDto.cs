namespace IdentidadServicio.Commons.Dtos;

public sealed class ResetearContrasenaRespuestaDto
{
    public Guid IdUsuario { get; set; }
    public string CorreoDestino { get; set; } = string.Empty;
    public string Mensaje { get; set; } = string.Empty;
}
