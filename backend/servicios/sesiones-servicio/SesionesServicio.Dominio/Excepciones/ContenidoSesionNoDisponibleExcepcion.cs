namespace SesionesServicio.Dominio.Excepciones;

// HU34/5.2 — Se lanza cuando una sesión existe pero el contenido
// asociado en juegos-servicio ya no está disponible (404). El
// middleware la traduce a HTTP 409 con mensaje claro para que el
// frontend pueda mostrarlo en pantalla.
public sealed class ContenidoSesionNoDisponibleExcepcion : Exception
{
    public ContenidoSesionNoDisponibleExcepcion(string mensaje) : base(mensaje) { }
}
