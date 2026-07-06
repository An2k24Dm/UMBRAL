using Microsoft.AspNetCore.SignalR;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Commons.Dtos;
using PartidasServicio.Commons.Dtos.TiempoReal;
using PartidasServicio.Infraestructura.TiempoReal.Hubs;

namespace PartidasServicio.Infraestructura.TiempoReal;

public sealed class NotificadorPartidasTiempoReal : INotificadorPartidasTiempoReal
{
    private readonly IHubContext<PartidasHub> _hub;

    public NotificadorPartidasTiempoReal(IHubContext<PartidasHub> hub)
    {
        _hub = hub;
    }

    public Task NotificarPuntajeActualizadoAsync(
        Guid sesionId,
        IReadOnlyList<RankingEntradaDto> ranking,
        CancellationToken cancelacion)
    {
        var dto = new PuntajeActualizadoDto
        {
            SesionId = sesionId,
            FechaEventoUtc = DateTime.UtcNow,
            Ranking = ranking
        };

        return _hub.Clients
            .Group(PartidasHub.GrupoPartida(sesionId))
            .SendAsync("PuntajeActualizado", dto, cancelacion);
    }
}
