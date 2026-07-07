using PartidasServicio.Dominio.Entidades;

namespace PartidasServicio.Dominio.Abstract;

public interface IRepositorioPartidas
{
    Task<Partida?> ObtenerPorSesionIdAsync(Guid sesionId, CancellationToken cancelacion);
    Task GuardarAsync(Partida partida, CancellationToken cancelacion);
}
