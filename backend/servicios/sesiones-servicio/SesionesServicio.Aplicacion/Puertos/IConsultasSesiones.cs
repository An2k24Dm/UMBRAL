using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Commons.Dtos.Sesiones;

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
    Task<IReadOnlyList<Sesion>> ListarActivasConEtapaVencidaAsync(
        DateTime ahoraUtc,
        CancellationToken cancelacion);

    Task<IReadOnlyList<Sesion>> ListarActivasConPreparacionVencidaAsync(
        DateTime ahoraUtc,
        CancellationToken cancelacion);

    Task<IReadOnlyList<Sesion>> ListarActivasConCierrePendienteVencidoAsync(
        DateTime ahoraUtc,
        CancellationToken cancelacion);

    Task<IReadOnlyList<Sesion>> ListarActivasConDuracionVencidaAsync(
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
public sealed record MiParticipacionProyeccion(
    Guid SesionId,
    string NombreSesion,
    string Modo,
    DateTime? FechaInicioUtc,
    DateTime? FechaFinalizacionUtc,
    int Puntaje);
