namespace JuegosServicio.Aplicacion.Puertos;

// Devuelve el token Bearer del request HTTP actual para que el
// adaptador hacia sesiones-servicio pueda reenviarlo. Juegos-servicio
// no emite tokens propios: reutiliza el del usuario autenticado. La
// implementación con HttpContext vive en la capa API; Aplicación se
// queda con la abstracción para poder probarla con dobles.
public interface IPropagadorTokenActual
{
    string? ObtenerTokenActual();
}
