namespace SesionesServicio.Aplicacion.Puertos;

public interface IUnidadTrabajoSesiones
{
    Task GuardarCambiosAsync(CancellationToken cancelacion);
}
