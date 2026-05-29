namespace SesionesServicio.Aplicacion.Puertos;

// Devuelve el token Bearer del request HTTP actual para reenviarlo a
// otros microservicios. Sesiones-servicio no emite tokens propios:
// reusa el token del usuario autenticado para llamar a juegos-servicio.
public interface IPropagadorTokenActual
{
    string? ObtenerTokenActual();
}
