using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IRepositorioSesiones
{
    Task AgregarAsync(Sesion sesion, CancellationToken cancelacion);

    Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);

    // Listado en orden descendente por fecha programada. La paginación
    // y filtros avanzados se postergan a historias posteriores; aquí
    // alcanza con una lectura simple para el panel del Operador.
    Task<IReadOnlyList<Sesion>> ListarAsync(CancellationToken cancelacion);
}
