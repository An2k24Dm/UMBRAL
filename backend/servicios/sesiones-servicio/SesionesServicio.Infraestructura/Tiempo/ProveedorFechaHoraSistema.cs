using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Infraestructura.Tiempo;

public sealed class ProveedorFechaHoraSistema : IProveedorFechaHora
{
    public DateTime ObtenerFechaHoraUtc() => DateTime.UtcNow;
}
