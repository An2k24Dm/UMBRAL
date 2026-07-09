using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Puertos;

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
    Task<IReadOnlyList<Sesion>> ListarActivasConTiempoVencidoAsync(
        DateTime ahoraUtc,
        CancellationToken cancelacion);
    Task<SesionParticipacionActivaDto?> ObtenerParticipacionActivaDeParticipanteAsync(
        Guid participanteIdentidadId,
        CancellationToken cancelacion);
    Task<IReadOnlyList<MiParticipacionProyeccion>> ListarParticipacionesFinalizadasAsync(
        Guid participanteIdentidadId,
        int limite,
        CancellationToken cancelacion);
}
public sealed record SesionParticipacionActivaDto(
    Guid SesionId,
    string NombreSesion,
    EstadoSesion Estado,
    ModoSesion Modo,
    Guid? EquipoId,
    string? EquipoNombre);

public sealed record MiParticipacionProyeccion(
    Guid SesionId,
    string NombreSesion,
    string Modo,
    DateTime? FechaInicioUtc,
    DateTime? FechaFinalizacionUtc,
    int Puntaje);
