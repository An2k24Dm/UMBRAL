using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Dominio.Abstract;

public interface IRepositorioSesiones
{
    Task AgregarAsync(Sesion sesion, CancellationToken cancelacion);
    Task ActualizarAsync(Sesion sesion, CancellationToken cancelacion);
    Task EliminarAsync(Sesion sesion, CancellationToken cancelacion);
    Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);
    Task<Sesion?> ObtenerPorCodigoAsync(string codigo, CancellationToken cancelacion);
    Task<bool> ExisteSesionVigentePorMisionAsync(
        Guid misionId,
        CancellationToken cancelacion);
}
