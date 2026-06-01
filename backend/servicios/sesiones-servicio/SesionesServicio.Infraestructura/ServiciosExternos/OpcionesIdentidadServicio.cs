namespace SesionesServicio.Infraestructura.ServiciosExternos;

// HU34 — URL base de identidad-servicio para consultar el rol de los
// creadores de sesiones. En docker es http://identidad-servicio:8080
// y en local http://localhost:5001.
public sealed class OpcionesIdentidadServicio
{
    public const string Seccion = "ServiciosExternos:IdentidadServicio";

    public string Url { get; set; } = string.Empty;
}
