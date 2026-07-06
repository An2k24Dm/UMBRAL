namespace PartidasServicio.Aplicacion.Puertos;

public interface IUnidadTrabajoPartidas
{
    Task GuardarCambiosAsync(CancellationToken cancelacion);
}
