namespace SesionesServicio.Aplicacion.Puertos;

public interface IProcesadorVencimientosEtapas
{
    Task<int> EjecutarCicloAsync(CancellationToken cancelacion);
}
