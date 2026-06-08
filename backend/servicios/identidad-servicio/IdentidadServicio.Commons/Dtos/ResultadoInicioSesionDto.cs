namespace IdentidadServicio.Commons.Dtos;

public sealed class ResultadoInicioSesionDto
{
    public string TokenAcceso { get; set; } = string.Empty;
    public string TokenRefresco { get; set; } = string.Empty;
    public int ExpiraEn { get; set; }
    public string TipoToken { get; set; } = "Bearer";
    public UsuarioAutenticadoDto Usuario { get; set; } = new();
    public string RutaRedireccion { get; set; } = string.Empty;

    // True cuando el usuario interno (Operador/Administrador) inició sesión
    // con una contraseña temporal (alta administrativa o reset). El frontend
    // debe redirigir a la pantalla propia de cambio obligatorio en lugar de
    // dejarlo entrar al panel. Para Participante siempre es false.
    public bool RequiereCambioContrasena { get; set; }
}
