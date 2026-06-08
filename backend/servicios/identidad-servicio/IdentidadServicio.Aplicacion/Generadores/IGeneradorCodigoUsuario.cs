namespace IdentidadServicio.Aplicacion.Generadores;

public interface IGeneradorCodigoUsuario
{
    Task<string> GenerarCodigoOperadorAsync(CancellationToken cancelacion);
    Task<string> GenerarCodigoAdministradorAsync(CancellationToken cancelacion);
}
