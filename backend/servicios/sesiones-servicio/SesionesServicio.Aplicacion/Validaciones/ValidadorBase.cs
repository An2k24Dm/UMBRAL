using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Aplicacion.Validaciones;

// Misma estructura "template method" usada en identidad-servicio: el
// validador concreto sólo expresa sus reglas y la clase base se encarga
// de construir/retornar el ResultadoValidacion.
public abstract class ValidadorBase<TSolicitud> : IValidador<TSolicitud>
{
    public ResultadoValidacion Validar(TSolicitud solicitud)
    {
        var resultado = ResultadoValidacion.Exitoso();
        ValidarSolicitud(solicitud, resultado);
        return resultado;
    }

    protected abstract void ValidarSolicitud(
        TSolicitud solicitud,
        ResultadoValidacion resultado);
}
