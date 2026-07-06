using PartidasServicio.Aplicacion.Puertos;

namespace PartidasServicio.Aplicacion.Validaciones;

public abstract class ValidadorBase<TSolicitud> : IValidador<TSolicitud>
{
    public ResultadoValidacion Validar(TSolicitud solicitud)
    {
        var resultado = ResultadoValidacion.Exitoso();
        ValidarSolicitud(solicitud, resultado);
        return resultado;
    }

    protected abstract void ValidarSolicitud(TSolicitud solicitud, ResultadoValidacion resultado);
}
