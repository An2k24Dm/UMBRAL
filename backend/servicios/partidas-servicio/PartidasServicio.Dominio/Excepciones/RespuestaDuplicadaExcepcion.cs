namespace PartidasServicio.Dominio.Excepciones;

public sealed class RespuestaDuplicadaExcepcion : ExcepcionDominio
{
    public RespuestaDuplicadaExcepcion()
        : base("Ya existe una respuesta registrada para esta pregunta.") { }
}
