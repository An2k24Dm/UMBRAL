using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Dominio.Abstract;

public interface IRepositorioPenalizacionesSesion
{
    Task AgregarAsync(PenalizacionSesion penalizacion, CancellationToken cancelacion);

    Task<PenalizacionSesion?> ObtenerPorEventoIdAsync(
        Guid eventoId, CancellationToken cancelacion);

    Task ActualizarAsync(PenalizacionSesion penalizacion, CancellationToken cancelacion);
}
