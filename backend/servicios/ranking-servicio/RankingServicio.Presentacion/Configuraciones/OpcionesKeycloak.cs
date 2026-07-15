namespace RankingServicio.Presentacion.Configuraciones;

public sealed class OpcionesKeycloak
{
    public const string Seccion = "Keycloak";

    public string Authority { get; set; } = string.Empty;

    public string MetadataAddress
        => $"{Authority.TrimEnd('/')}/.well-known/openid-configuration";
}
