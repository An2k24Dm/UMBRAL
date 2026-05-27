namespace IdentidadServicio.Dominio.Excepciones;

// Caso simétrico de UsuarioYaInactivoExcepcion: se lanza cuando se intenta
// activar a un usuario que ya está Activo. Es un caso de negocio (no un
// error de sistema) y el ManejadorErroresMiddleware lo mapea a HTTP 400
// con código USUARIO_YA_ACTIVO.
public sealed class UsuarioYaActivoExcepcion : Exception
{
    public UsuarioYaActivoExcepcion()
        : base("La cuenta ya se encuentra activa.") { }
}
