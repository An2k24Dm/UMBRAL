using Microsoft.AspNetCore.SignalR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos.TiempoReal;
using SesionesServicio.Infraestructura.TiempoReal.Hubs;

namespace SesionesServicio.Infraestructura.TiempoReal;

public sealed class NotificadorSesionesTiempoReal : INotificadorSesionesTiempoReal
{
    private readonly IHubContext<SesionesHub> _hub;

    public NotificadorSesionesTiempoReal(IHubContext<SesionesHub> hub)
    {
        _hub = hub;
    }

    public Task NotificarParticipantesSesionActualizadosAsync(
        Guid sesionId,
        CancellationToken cancelacion)
    {
        var dto = new ParticipantesSesionActualizadosDto
        {
            SesionId = sesionId,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("ParticipantesSesionActualizados", dto, cancelacion);
    }

    public Task NotificarEquiposSesionActualizadosAsync(
        Guid sesionId,
        Guid? equipoId,
        CancellationToken cancelacion)
    {
        var dto = new EquiposSesionActualizadosDto
        {
            SesionId = sesionId,
            EquipoId = equipoId,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("EquiposSesionActualizados", dto, cancelacion);
    }

    public async Task NotificarEquipoActualizadoAsync(
        Guid sesionId,
        Guid equipoId,
        CancellationToken cancelacion)
    {
        var dto = new EquipoActualizadoTiempoRealDto
        {
            SesionId = sesionId,
            EquipoId = equipoId,
            FechaEventoUtc = DateTime.UtcNow
        };

        await _hub.Clients
            .Group(SesionesHub.GrupoEquipo(equipoId))
            .SendAsync("EquipoActualizado", dto, cancelacion);

        await _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("EquipoActualizado", dto, cancelacion);
    }
}
