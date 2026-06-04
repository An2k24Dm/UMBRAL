namespace IdentidadServicio.Aplicacion.Validaciones;

public interface IValidadorAsincrono<in TSolicitud>
{
    Task<ResultadoValidacion> ValidarAsync(
        TSolicitud solicitud,
        CancellationToken cancelacion);
}
