using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

// Confirma todos los cambios acumulados en el ContextoIdentidad como una sola
// transacción. EF Core abre una transacción implícita por SaveChanges, lo que
// es suficiente para los flujos actuales (HU02/HU03/HU09). Si una historia
// futura necesita transacciones que abarquen varias llamadas a SaveChanges,
// este puerto es el punto natural donde introducir BeginTransactionAsync.
public sealed class UnidadTrabajoIdentidad : IUnidadTrabajoIdentidad
{
    private readonly ContextoIdentidad _contexto;

    public UnidadTrabajoIdentidad(ContextoIdentidad contexto)
    {
        _contexto = contexto;
    }

    public Task GuardarCambiosAsync(CancellationToken cancelacion)
        => _contexto.SaveChangesAsync(cancelacion);
}
