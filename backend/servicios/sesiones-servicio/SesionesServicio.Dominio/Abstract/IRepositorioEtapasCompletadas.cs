namespace SesionesServicio.Dominio.Abstract;

public interface IRepositorioEtapasCompletadas
{
    Task<bool> RegistrarAsync(Guid sesionId, Guid etapaId, DateTime fechaUtc, CancellationToken cancelacion);
    Task<int> ContarAsync(Guid sesionId, CancellationToken cancelacion);
    Task<IReadOnlyList<Guid>> ObtenerCompletadasAsync(Guid sesionId, CancellationToken cancelacion);
}
