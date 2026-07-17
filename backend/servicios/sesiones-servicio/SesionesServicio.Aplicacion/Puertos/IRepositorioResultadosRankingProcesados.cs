namespace SesionesServicio.Aplicacion.Puertos;

public interface IRepositorioResultadosRankingProcesados
{
    Task<bool> ExisteAsync(
        Guid eventoIdOrigen,
        string tipoResultado,
        CancellationToken cancelacion);

    Task RegistrarAsync(
        Guid eventoIdOrigen,
        string tipoResultado,
        DateTime procesadoEnUtc,
        CancellationToken cancelacion);
}
