using Microsoft.AspNetCore.SignalR;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Commons.Dtos.Eventos.Salida;
using RankingServicio.Commons.Dtos.TiempoReal;

namespace RankingServicio.Infraestructura.TiempoReal;

public sealed class NotificadorRankingTiempoReal : INotificadorRankingTiempoReal
{
    private readonly IHubContext<RankingHub> _hub;

    public NotificadorRankingTiempoReal(IHubContext<RankingHub> hub)
    {
        _hub = hub;
    }

    public Task NotificarPuntajeCalculadoAsync(
        PuntajeCalculadoDto puntaje, CancellationToken cancelacion)
        => _hub.Clients
            .Group($"sesion:{puntaje.SesionId}")
            .SendAsync("PuntajeCalculado", puntaje, cancelacion);

    public Task NotificarRankingParticipantesActualizadoAsync(
        Guid sesionId, CancellationToken cancelacion)
        => _hub.Clients
            .Group($"sesion:{sesionId}")
            .SendAsync("RankingParticipantesActualizado", sesionId, cancelacion);

    public Task NotificarRankingEquiposActualizadoAsync(
        Guid sesionId, CancellationToken cancelacion)
        => _hub.Clients
            .Group($"sesion:{sesionId}")
            .SendAsync("RankingEquiposActualizado", sesionId, cancelacion);

    public Task NotificarPenalizacionAplicadaAsync(
        PenalizacionAplicadaNotificacionDto penalizacion, CancellationToken cancelacion)
        => _hub.Clients
            .Group($"sesion:{penalizacion.SesionId}")
            .SendAsync("PenalizacionAplicada", penalizacion, cancelacion);
}
