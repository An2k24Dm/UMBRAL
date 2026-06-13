using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Puertos;

// Puerto de consultas/lectura sobre sesiones. Vive en Aplicación porque
// expone listados, filtros y proyecciones orientadas a casos de uso y
// pantallas, no operaciones del agregado. La implementación concreta en
// Infraestructura es la misma que la del repositorio del dominio.
public interface IConsultasSesiones
{
    Task<IReadOnlyList<Sesion>> ListarAsync(
        EstadoSesion? estado,
        Guid? operadorCreadorId,
        CancellationToken cancelacion);
    Task<IReadOnlyList<Sesion>> ListarProgramadasVencidasAsync(
        DateTime fechaActualUtc,
        CancellationToken cancelacion);
    Task<IReadOnlyList<Sesion>> ListarDisponiblesParaParticipanteAsync(
        string? busqueda,
        string? tipoSesion,
        CancellationToken cancelacion);
}
