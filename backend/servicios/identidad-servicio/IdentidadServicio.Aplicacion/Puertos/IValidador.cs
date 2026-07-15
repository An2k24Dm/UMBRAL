using IdentidadServicio.Aplicacion.Validaciones;

namespace IdentidadServicio.Aplicacion.Puertos;

public interface IValidador<in TSolicitud>
{
    ResultadoValidacion Validar(TSolicitud solicitud);
}
