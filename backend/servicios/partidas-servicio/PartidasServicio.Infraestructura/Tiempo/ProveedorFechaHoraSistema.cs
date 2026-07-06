using PartidasServicio.Aplicacion.Puertos;

namespace PartidasServicio.Infraestructura.Tiempo;

public sealed class ProveedorFechaHoraSistema : IProveedorFechaHora
{
    public DateTime ObtenerFechaHoraUtc() => DateTime.UtcNow;
}
