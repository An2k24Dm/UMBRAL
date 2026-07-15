namespace SesionesServicio.Infraestructura.Configuraciones;

public sealed class OpcionesPreparacionSesiones
{
    public const string Seccion = "Sesiones";

    public int IntervaloPreparacionSegundos { get; set; } = 60;
}
