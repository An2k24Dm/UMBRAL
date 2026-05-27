using IdentidadServicio.Aplicacion.Puertos;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

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
