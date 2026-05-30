namespace SesionesServicio.Infraestructura.ServiciosExternos;

public sealed class OpcionesJuegosServicio
{
    public const string Seccion = "ServiciosExternos:JuegosServicio";

    // URL base completa, incluyendo esquema y puerto. En docker el valor
    // típico es http://juegos-servicio:8080 y en local http://localhost:5002.
    public string Url { get; set; } = string.Empty;
}
