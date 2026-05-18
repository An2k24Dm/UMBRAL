namespace IdentidadServicio.Aplicacion.Puertos;

// Abstracción del reloj. Su única implementación está autorizada a usar
// DateTime.UtcNow. Cualquier otra capa debe inyectar este puerto.
public interface IProveedorFechaHora
{
    DateTime ObtenerFechaHoraUtc();
}
