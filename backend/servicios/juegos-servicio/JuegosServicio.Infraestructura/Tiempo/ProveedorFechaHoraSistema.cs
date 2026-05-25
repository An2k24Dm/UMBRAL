using JuegosServicio.Aplicacion.Puertos;

namespace JuegosServicio.Infraestructura.Tiempo;

public sealed class ProveedorFechaHoraSistema : IProveedorFechaHora
{
    public DateTime ObtenerFechaHoraUtc() => DateTime.UtcNow;
}
