using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Puertos;

// Puerto específico para Administradores. Lo usan HU02 (alta) y el generador
// de códigos AD-### (último código asignado).
public interface IRepositorioAdministradores
{
    // HU02 — alta. La confirmación se hace por IUnidadTrabajoIdentidad.
    Task AgregarAsync(
        Administrador administrador, string idKeycloak, CancellationToken cancelacion);

    // Generador HU02 — último código AD-### asignado o null si no hay ninguno.
    Task<string?> ObtenerUltimoCodigoAsync(CancellationToken cancelacion);
}
