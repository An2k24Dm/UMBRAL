namespace JuegosServicio.Dominio.Excepciones;

public sealed class ContenidoUsadoEnMisionActivaExcepcion : ExcepcionDominio
{
    public ContenidoUsadoEnMisionActivaExcepcion()
        : base("No se puede modificar este contenido porque está siendo usado en una misión activa.") { }
}
