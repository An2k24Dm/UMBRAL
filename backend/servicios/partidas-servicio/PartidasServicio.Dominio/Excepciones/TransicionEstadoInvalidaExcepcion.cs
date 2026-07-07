namespace PartidasServicio.Dominio.Excepciones;

public sealed class TransicionEstadoInvalidaExcepcion : ExcepcionDominio
{
    public TransicionEstadoInvalidaExcepcion(string estadoActual, string operacion)
        : base($"No se puede ejecutar '{operacion}' estando en estado '{estadoActual}'.") { }
}
