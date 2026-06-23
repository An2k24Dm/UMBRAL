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
}
public sealed record SesionParticipacionActivaDto(
    Guid SesionId,
    string NombreSesion,
    EstadoSesion Estado,
    ModoSesion Modo,
    Guid? EquipoId,
    string? EquipoNombre);
