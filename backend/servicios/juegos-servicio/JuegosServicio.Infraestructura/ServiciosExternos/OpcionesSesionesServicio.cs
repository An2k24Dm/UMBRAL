namespace JuegosServicio.Infraestructura.ServiciosExternos;

public sealed class OpcionesSesionesServicio
{
    public const string Seccion = "ServiciosExternos:SesionesServicio";

    // URL base completa, incluyendo esquema y puerto. En docker el
    // valor típico es http://sesiones-servicio:8080 y en local
    // http://localhost:5003.
    public string Url { get; set; } = string.Empty;
}
