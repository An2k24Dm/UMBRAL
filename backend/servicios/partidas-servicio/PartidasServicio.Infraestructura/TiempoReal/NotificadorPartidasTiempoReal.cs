using Microsoft.AspNetCore.SignalR;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Commons.Dtos;
using PartidasServicio.Commons.Dtos.TiempoReal;
using PartidasServicio.Infraestructura.TiempoReal.Hubs;

namespace PartidasServicio.Infraestructura.TiempoReal;

public sealed class NotificadorPartidasTiempoReal : INotificadorPartidasTiempoReal
{
    private readonly IHubContext<PartidasHub> _hub;

    public NotificadorPartidasTiempoReal(IHubContext<PartidasHub> hub) => _hub = hub;

    public Task NotificarCambioEstadoPartidaAsync(Guid sesionId, string estado, CancellationToken cancelacion)
    {
        var dto = new EstadoPartidaCambiadoDto
        {
            SesionId = sesionId,
            Estado = estado,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .Group(PartidasHub.GrupoPartida(sesionId))
            .SendAsync("EstadoPartidaCambiado", dto, cancelacion);
    }

    public Task NotificarRespuestaRegistradaAsync(
        Guid sesionId, Guid preguntaId, Guid? equipoId,
        bool esCorrecta, int puntosGanados, CancellationToken cancelacion)
    {
        var dto = new RespuestaRegistradaDto
        {
            SesionId = sesionId,
            PreguntaId = preguntaId,
            EquipoId = equipoId,
            EsCorrecta = esCorrecta,
            PuntosGanados = puntosGanados,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .Group(PartidasHub.GrupoPartida(sesionId))
            .SendAsync("RespuestaRegistrada", dto, cancelacion);
    }

    public Task NotificarPuntajeActualizadoAsync(
        Guid sesionId, IReadOnlyList<RankingEntradaDto> ranking, CancellationToken cancelacion)
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
