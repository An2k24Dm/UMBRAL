using PartidasServicio.Aplicacion.Validaciones;

namespace PartidasServicio.Aplicacion.Puertos;

public interface IValidador<in TSolicitud>
{
    ResultadoValidacion Validar(TSolicitud solicitud);
}
