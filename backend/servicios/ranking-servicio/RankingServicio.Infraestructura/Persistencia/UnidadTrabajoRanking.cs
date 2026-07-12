using Microsoft.EntityFrameworkCore;
using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Infraestructura.Persistencia;

public sealed class UnidadTrabajoRanking : IUnidadTrabajoRanking
{
    private readonly ContextoRanking _contexto;

    public UnidadTrabajoRanking(ContextoRanking contexto)
    {
        _contexto = contexto;
    }

    public Task GuardarCambiosAsync(CancellationToken cancelacion)
        => _contexto.SaveChangesAsync(cancelacion);

    public async Task EjecutarEnTransaccionAsync(
        Func<CancellationToken, Task> operacion, CancellationToken cancelacion)
    {
        var estrategia = _contexto.Database.CreateExecutionStrategy();
        await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await _contexto.Database
                .BeginTransactionAsync(cancelacion);
            try
            {
                await operacion(cancelacion);
                await _contexto.SaveChangesAsync(cancelacion);
                await transaccion.CommitAsync(cancelacion);
            }
            catch
            {
                await transaccion.RollbackAsync(cancelacion);
                throw;
            }
        });
    }
}
