namespace RankingServicio.Aplicacion.Puertos;

public interface IUnidadTrabajoRanking
{
    Task GuardarCambiosAsync(CancellationToken cancelacion);
    Task EjecutarEnTransaccionAsync(
        Func<CancellationToken, Task> operacion, CancellationToken cancelacion);
}
