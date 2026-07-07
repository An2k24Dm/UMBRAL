using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.Aplicacion.Puertos;

public interface INotificadorPartidasTiempoReal
{
    Task NotificarCambioEstadoPartidaAsync(Guid sesionId, string estado, CancellationToken cancelacion);

    Task NotificarRespuestaRegistradaAsync(
        Guid sesionId,
        Guid preguntaId,
        Guid? equipoId,
        bool esCorrecta,
        int puntosGanados,
        CancellationToken cancelacion);

    Task NotificarPuntajeActualizadoAsync(
        Guid sesionId,
        IReadOnlyList<RankingEntradaDto> ranking,
        CancellationToken cancelacion);
}
