using SesionesServicio.Aplicacion.Puertos;
using Microsoft.EntityFrameworkCore;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class UnidadTrabajoSesiones : IUnidadTrabajoSesiones
{
    private readonly ContextoSesiones _contexto;

    public UnidadTrabajoSesiones(ContextoSesiones contexto)
    {
        _contexto = contexto;
    }

    public Task GuardarCambiosAsync(CancellationToken cancelacion)
        => _contexto.SaveChangesAsync(cancelacion);

    public async Task EjecutarEnTransaccionAsync(
        Func<CancellationToken, Task> operacion,
        CancellationToken cancelacion)
    {
        var estrategia = _contexto.Database.CreateExecutionStrategy();
        await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await _contexto.Database
                .BeginTransactionAsync(cancelacion);
            await operacion(cancelacion);
            await _contexto.SaveChangesAsync(cancelacion);
            await transaccion.CommitAsync(cancelacion);
        });
    }
}
