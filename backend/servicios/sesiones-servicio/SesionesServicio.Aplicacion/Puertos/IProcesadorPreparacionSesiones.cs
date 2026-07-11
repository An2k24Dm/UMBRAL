namespace SesionesServicio.Aplicacion.Puertos;

public interface IProcesadorPreparacionSesiones
{
    Task EjecutarCicloAsync(CancellationToken cancelacion);
}
