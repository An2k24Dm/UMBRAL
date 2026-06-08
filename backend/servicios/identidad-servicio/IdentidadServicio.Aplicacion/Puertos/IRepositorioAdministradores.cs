using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Puertos;

public interface IRepositorioAdministradores
{
    Task AgregarAsync(
        Administrador administrador, string idKeycloak, CancellationToken cancelacion);
    Task<string?> ObtenerUltimoCodigoAsync(CancellationToken cancelacion);
}
