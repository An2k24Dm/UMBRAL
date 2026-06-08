namespace IdentidadServicio.Aplicacion.Puertos;

public interface IUnidadTrabajoIdentidad
{
    Task GuardarCambiosAsync(CancellationToken cancelacion);
}
