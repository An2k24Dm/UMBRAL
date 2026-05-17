using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Infraestructura.Tiempo;

// Única clase autorizada a usar DateTime.UtcNow.
public sealed class ProveedorFechaHoraSistema : IProveedorFechaHora
{
    public DateTime ObtenerFechaHoraUtc() => DateTime.UtcNow;
}
