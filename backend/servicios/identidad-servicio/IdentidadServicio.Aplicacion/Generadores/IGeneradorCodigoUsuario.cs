namespace IdentidadServicio.Aplicacion.Generadores;

// Genera el siguiente código correlativo de Operador (OP-###) o Administrador
// (AD-###) consultando al repositorio. El frontend ya no envía el código:
// el backend es la fuente de verdad.
public interface IGeneradorCodigoUsuario
{
    Task<string> GenerarCodigoOperadorAsync(CancellationToken cancelacion);
    Task<string> GenerarCodigoAdministradorAsync(CancellationToken cancelacion);
}
