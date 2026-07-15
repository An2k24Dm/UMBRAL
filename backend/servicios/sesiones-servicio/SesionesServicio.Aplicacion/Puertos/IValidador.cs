using SesionesServicio.Aplicacion.Validaciones;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IValidador<in TSolicitud>
{
    ResultadoValidacion Validar(TSolicitud solicitud);
}
