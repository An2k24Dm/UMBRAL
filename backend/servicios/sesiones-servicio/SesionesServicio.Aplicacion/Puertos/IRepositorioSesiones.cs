using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IRepositorioSesiones
{
    Task AgregarAsync(Sesion sesion, CancellationToken cancelacion);

    Task ActualizarAsync(Sesion sesion, CancellationToken cancelacion);

    Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion);

    // Trae el listado completo de sesiones. La regla de visibilidad por
    // rol (Administrador ve todo, Operador ve las propias) se aplica en
    // el manejador filtrando por OperadorCreadorId.
    Task<IReadOnlyList<Sesion>> ListarAsync(
        EstadoSesion? estado,
        Guid? operadorCreadorId,
        CancellationToken cancelacion);

    // Sesiones Programadas cuya FechaProgramada ya pasó. Las usa el
    // HostedService para pasarlas a EnPreparacion. El filtro corre en BD.
    Task<IReadOnlyList<Sesion>> ListarProgramadasVencidasAsync(
        DateTime fechaActualUtc,
        CancellationToken cancelacion);

    // Sesiones vigentes (Programada, EnPreparacion, Activa, Pausada)
    // asociadas a una misión determinada. La consume juegos-servicio
    // antes de desactivar/eliminar una misión.
    Task<bool> ExisteSesionVigentePorMisionAsync(
        Guid misionId,
        CancellationToken cancelacion);

    // Sesiones que el Participante puede ver y eventualmente unirse.
    // Filtro de estado fijo: Programada, EnPreparacion y Activa (las
    // únicas en las que tiene sentido que un Participante entre).
    // Filtros opcionales:
    //   * busqueda: subcadena del nombre, case-insensitive.
    //   * tipoSesion: "Individual" / "Grupal" / null (todas).
    // Orden: fechaProgramada ascendente (primero las más próximas).
    Task<IReadOnlyList<Sesion>> ListarDisponiblesParaParticipanteAsync(
        string? busqueda,
        string? tipoSesion,
        CancellationToken cancelacion);
}
