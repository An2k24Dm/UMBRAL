using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IRepositorioSesiones
{
    Task AgregarAsync(Sesion sesion, CancellationToken cancelacion);

    Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);

    // Listado en orden descendente por fecha programada. La paginación
    // y filtros avanzados se postergan a historias posteriores; aquí
    // alcanza con una lectura simple para el panel del Operador.
    Task<IReadOnlyList<Sesion>> ListarAsync(CancellationToken cancelacion);

    // Consulta usada por el endpoint que sirve a juegos-servicio para
    // bloquear la desactivación de contenido. Devuelve true si existe
    // al menos una sesión en estado vigente (Programada, EnPreparacion,
    // Activa, Pausada) asociada al contenido indicado.
    Task<bool> ExisteSesionVigentePorContenidoAsync(
        TipoJuego tipoJuego,
        Guid contenidoJuegoId,
        CancellationToken cancelacion);
}
