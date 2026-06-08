namespace SesionesServicio.Presentacion.Configuraciones;

public sealed class OpcionesKeycloak
{
    public const string Seccion = "Keycloak";

    public string Authority { get; set; } = string.Empty;

    public string? MetadataAddress { get; set; }
}
