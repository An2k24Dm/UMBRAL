using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Infraestructura.Tiempo;

public sealed class ProveedorFechaHoraUtc : IProveedorFechaHora
{
    public DateTime ObtenerFechaHoraUtc() => DateTime.UtcNow;
}
