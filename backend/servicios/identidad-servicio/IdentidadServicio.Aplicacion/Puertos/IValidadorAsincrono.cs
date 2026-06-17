using IdentidadServicio.Aplicacion.Validaciones;

namespace IdentidadServicio.Aplicacion.Puertos;

public interface IValidadorAsincrono<in TSolicitud>
{
    Task<ResultadoValidacion> ValidarAsync(
        TSolicitud solicitud,
        CancellationToken cancelacion);
}
