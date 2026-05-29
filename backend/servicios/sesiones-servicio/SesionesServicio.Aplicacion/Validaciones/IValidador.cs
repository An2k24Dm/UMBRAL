namespace SesionesServicio.Aplicacion.Validaciones;

public interface IValidador<in TSolicitud>
{
    ResultadoValidacion Validar(TSolicitud solicitud);
}
