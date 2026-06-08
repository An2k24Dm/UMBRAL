namespace IdentidadServicio.Dominio.Excepciones;

public sealed class UsuarioYaInactivoExcepcion : Exception
{
    public UsuarioYaInactivoExcepcion()
        : base("La cuenta ya se encuentra inactiva.") { }
}
