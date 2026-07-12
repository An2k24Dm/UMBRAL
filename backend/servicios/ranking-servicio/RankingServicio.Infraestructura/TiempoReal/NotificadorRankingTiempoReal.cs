using Microsoft.AspNetCore.SignalR;
using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Infraestructura.TiempoReal;

public sealed class NotificadorRankingTiempoReal : INotificadorRankingTiempoReal
{
    private readonly IHubContext<RankingHub> _hub;

    public NotificadorRankingTiempoReal(IHubContext<RankingHub> hub)
    {
        _hub = hub;
    }

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
}
