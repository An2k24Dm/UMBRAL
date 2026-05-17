namespace IdentidadServicio.Dominio.Excepciones;

public sealed class RolNoValidoExcepcion : Exception
{
    public RolNoValidoExcepcion()
        : base("El usuario no posee un rol válido para acceder a la plataforma.") { }
}
