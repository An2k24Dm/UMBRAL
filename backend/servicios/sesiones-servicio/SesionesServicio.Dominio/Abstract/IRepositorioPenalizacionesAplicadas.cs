using SesionesServicio.Dominio.Eventos;

namespace SesionesServicio.Dominio.Abstract;

public interface IRepositorioPenalizacionesAplicadas
{
    Task AgregarAsync(PenalizacionAplicada penalizacion, CancellationToken cancelacion);

    Task<bool> ExistePorEventoIdAsync(Guid eventoId, CancellationToken cancelacion);

    Task<IReadOnlyList<PenalizacionAplicada>> ListarPorSesionAsync(
        Guid sesionId, CancellationToken cancelacion);

    Task<IReadOnlyList<PenalizacionAplicada>> ListarPorParticipanteAsync(
        Guid sesionId, Guid participanteIdentidadId, CancellationToken cancelacion);

    Task<IReadOnlyList<PenalizacionAplicada>> ListarPorEquipoAsync(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion);

    Task<int> SumarPuntosPorParticipanteAsync(
        Guid sesionId, Guid participanteIdentidadId, CancellationToken cancelacion);

    Task<int> SumarPuntosPorEquipoAsync(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion);
}
