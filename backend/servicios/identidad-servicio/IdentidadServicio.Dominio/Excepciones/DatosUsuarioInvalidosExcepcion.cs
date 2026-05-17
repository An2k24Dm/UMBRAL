namespace IdentidadServicio.Dominio.Excepciones;

public sealed class DatosUsuarioInvalidosExcepcion : Exception
{
    public DatosUsuarioInvalidosExcepcion(string mensaje) : base(mensaje) { }
}
