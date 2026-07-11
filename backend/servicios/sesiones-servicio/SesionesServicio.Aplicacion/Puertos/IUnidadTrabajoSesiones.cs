namespace SesionesServicio.Aplicacion.Puertos;

public interface IUnidadTrabajoSesiones
{
    Task GuardarCambiosAsync(CancellationToken cancelacion);
    Task EjecutarEnTransaccionAsync(
        Func<CancellationToken, Task> operacion,
        CancellationToken cancelacion);
}
