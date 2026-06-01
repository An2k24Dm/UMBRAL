using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IRepositorioSesiones
{
    Task AgregarAsync(Sesion sesion, CancellationToken cancelacion);

    Task ActualizarAsync(Sesion sesion, CancellationToken cancelacion);

    Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);

    // HU34 — Listado completo con filtros opcionales por TipoJuego y
    // Estado. Lo usa el Administrador (que ve todo) y también lo usa
    // el Operador como base: el manejador combina el resultado con
    // identidad-servicio para quedarse sólo con las creadas por él o
    // por algún Administrador.
    Task<IReadOnlyList<Sesion>> ListarAsync(
        TipoJuego? tipoJuego,
        EstadoSesion? estado,
        CancellationToken cancelacion);

    // HU34/5.1 — Sesiones Programadas cuya FechaProgramada ya pasó.
    // Las usa el HostedService para pasarlas a EnPreparacion. El
    // filtro corre en BD para no traer toda la tabla.
    Task<IReadOnlyList<Sesion>> ListarProgramadasVencidasAsync(
        DateTime fechaActualUtc,
        CancellationToken cancelacion);

    // Consulta usada por el endpoint que sirve a juegos-servicio para
    // bloquear la desactivación de contenido. Devuelve true si existe
    // al menos una sesión en estado vigente (Programada, EnPreparacion,
    // Activa, Pausada) asociada al contenido indicado.
    Task<bool> ExisteSesionVigentePorContenidoAsync(
        TipoJuego tipoJuego,
        Guid contenidoJuegoId,
        CancellationToken cancelacion);
}
