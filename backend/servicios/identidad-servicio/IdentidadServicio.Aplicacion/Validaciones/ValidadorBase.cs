namespace IdentidadServicio.Aplicacion.Validaciones;

// Clase base para validadores. Implementa el patrón "template method": expone
// un Validar() final que crea el ResultadoValidacion y delega la lógica
// específica del caso de uso al método protegido ValidarSolicitud.
//
// De esta manera cada validador concreto se concentra solo en las reglas que
// le son propias y no tiene que repetir la mecánica de armar/devolver el
// ResultadoValidacion.
public abstract class ValidadorBase<TSolicitud> : IValidador<TSolicitud>
{
    public ResultadoValidacion Validar(TSolicitud solicitud)
    {
        var resultado = ResultadoValidacion.Exitoso();
        ValidarSolicitud(solicitud, resultado);
        return resultado;
    }

    // Cada validador implementa solo las reglas particulares del caso de uso,
    // y opcionalmente delega las reglas comunes en IReglasValidacionUsuario.
    protected abstract void ValidarSolicitud(
        TSolicitud solicitud,
        ResultadoValidacion resultado);
}
