namespace RankingServicio.Aplicacion.Puertos;

public interface IRepositorioEventosProcesados
{
    Task<bool> ExisteAsync(Guid eventoId, string tipoEvento, CancellationToken cancelacion);
    Task RegistrarAsync(Guid eventoId, string tipoEvento, DateTime ahora, CancellationToken cancelacion);
}
