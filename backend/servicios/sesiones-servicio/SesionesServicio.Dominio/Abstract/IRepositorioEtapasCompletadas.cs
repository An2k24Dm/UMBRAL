namespace SesionesServicio.Dominio.Abstract;

public interface IRepositorioEtapasCompletadas
{
    Task RegistrarAsync(Guid sesionId, Guid etapaId, DateTime fechaUtc, CancellationToken cancelacion);
    Task<int> ContarAsync(Guid sesionId, CancellationToken cancelacion);
}
