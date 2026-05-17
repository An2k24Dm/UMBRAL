namespace IdentidadServicio.Dominio.Excepciones;

// Se lanza cuando un usuario intenta iniciar sesión desde una aplicación
// que no le corresponde según su rol (p. ej. Participante en web, o
// Administrador/Operador en móvil).
public sealed class AccesoNoPermitidoExcepcion : Exception
{
    public AccesoNoPermitidoExcepcion(string mensaje) : base(mensaje) { }
}
