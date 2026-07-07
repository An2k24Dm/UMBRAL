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
    Task<SesionParticipacionActivaDto?> ObtenerParticipacionActivaDeParticipanteAsync(
        Guid participanteIdentidadId,
        CancellationToken cancelacion);

    Task<EstadoPartidaParticipanteDto?> ObtenerEstadoPartidaAsync(
        Guid sesionId,
        Guid participanteIdentidadId,
        CancellationToken cancelacion);

    Task<NombresRankingDto> ObtenerNombresRankingAsync(
        Guid sesionId,
        CancellationToken cancelacion);
}
public sealed record SesionParticipacionActivaDto(
    Guid SesionId,
    string NombreSesion,
    EstadoSesion Estado,
    ModoSesion Modo,
    Guid? EquipoId,
    string? EquipoNombre);

public sealed record EstadoPartidaParticipanteDto(
    string Estado,
    bool ParticipanteInscrito,
    Guid? EquipoId);

public sealed class NombresRankingDto
{
    public List<NombreEquipoDto> Equipos { get; set; } = new();
    public List<NombreParticipanteDto> Participantes { get; set; } = new();
}

public sealed class NombreEquipoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public sealed class NombreParticipanteDto
{
    public Guid IdentidadId { get; set; }
    public string Alias { get; set; } = string.Empty;
}
