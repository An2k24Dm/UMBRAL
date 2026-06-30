namespace SesionesServicio.Infraestructura.Configuraciones;

// HU34/5.1 — Configuración del servicio en segundo plano que pasa las
// sesiones Programadas a EnPreparacion cuando su fecha vence. Si no se
// configura, el servicio corre cada 60 segundos.
public sealed class OpcionesPreparacionSesiones
{
    public const string Seccion = "Sesiones";

    public int IntervaloPreparacionSegundos { get; set; } = 60;
}
