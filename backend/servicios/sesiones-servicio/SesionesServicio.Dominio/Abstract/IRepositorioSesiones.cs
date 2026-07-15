using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Dominio.Abstract;

public interface IRepositorioSesiones
{
    Task AgregarAsync(Sesion sesion, CancellationToken cancelacion);
    Task ActualizarAsync(Sesion sesion, CancellationToken cancelacion);
    Task EliminarAsync(Sesion sesion, CancellationToken cancelacion);
    Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);
    Task<Sesion?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancelacion);
    // Resuelve la sesión (grupal) propietaria de un equipo. Se usa para
    // autorizar la suscripción al grupo de un equipo en tiempo real.
    Task<Sesion?> ObtenerPorEquipoIdAsync(Guid equipoId, CancellationToken cancelacion);
    Task<bool> ExisteSesionVigentePorMisionAsync(
        Guid misionId,
        CancellationToken cancelacion);
}
