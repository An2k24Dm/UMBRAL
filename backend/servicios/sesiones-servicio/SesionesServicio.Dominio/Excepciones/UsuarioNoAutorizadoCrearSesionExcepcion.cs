namespace SesionesServicio.Dominio.Excepciones;

public sealed class UsuarioNoAutorizadoCrearSesionExcepcion : Exception
{
    public UsuarioNoAutorizadoCrearSesionExcepcion(string mensaje) : base(mensaje) { }
}
