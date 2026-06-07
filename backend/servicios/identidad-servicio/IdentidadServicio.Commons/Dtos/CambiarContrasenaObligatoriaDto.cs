namespace IdentidadServicio.Commons.Dtos;

public sealed class CambiarContrasenaObligatoriaDto
{
    public string NuevaContrasena { get; set; } = string.Empty;
    public string ConfirmacionContrasena { get; set; } = string.Empty;
}

public sealed class CambiarContrasenaObligatoriaRespuestaDto
{
    public string Mensaje { get; set; } = string.Empty;
    public string RutaRedireccion { get; set; } = string.Empty;
}
