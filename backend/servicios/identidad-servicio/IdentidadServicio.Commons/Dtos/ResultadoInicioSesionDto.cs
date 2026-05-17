namespace IdentidadServicio.Commons.Dtos;

public sealed class ResultadoInicioSesionDto
{
    public string TokenAcceso { get; set; } = string.Empty;
    public string TokenRefresco { get; set; } = string.Empty;
    public int ExpiraEn { get; set; }
    public string TipoToken { get; set; } = "Bearer";
    public UsuarioAutenticadoDto Usuario { get; set; } = new();
    public string RutaRedireccion { get; set; } = string.Empty;
}
