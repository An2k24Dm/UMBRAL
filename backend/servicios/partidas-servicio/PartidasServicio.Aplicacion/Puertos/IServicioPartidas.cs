namespace PartidasServicio.Aplicacion.Puertos;

public interface IServicioPartidas
{
    Task IniciarPartidaAsync(Guid sesionId, CancellationToken cancelacion = default);
    Task PausarPartidaAsync(Guid sesionId, CancellationToken cancelacion = default);
    Task ReanudarPartidaAsync(Guid sesionId, CancellationToken cancelacion = default);
    Task FinalizarPartidaAsync(Guid sesionId, CancellationToken cancelacion = default);
    Task CancelarPartidaAsync(Guid sesionId, CancellationToken cancelacion = default);
}
