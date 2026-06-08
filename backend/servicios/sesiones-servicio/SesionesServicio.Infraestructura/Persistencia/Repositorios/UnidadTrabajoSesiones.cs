using SesionesServicio.Aplicacion.Puertos;

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
}
