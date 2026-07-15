using Microsoft.AspNetCore.SignalR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Infraestructura.TiempoReal.Hubs;

namespace SesionesServicio.Infraestructura.TiempoReal.Grupos;

public sealed class ServicioGruposSesionesTiempoReal : IServicioGruposSesionesTiempoReal
{
    private readonly IHubContext<SesionesHub> _hub;

    public ServicioGruposSesionesTiempoReal(IHubContext<SesionesHub> hub)
    {
        _hub = hub;
    }

    public Task UnirseAListadoAsync(
        string connectionId, Guid? usuarioId, IReadOnlyCollection<string> roles,
        CancellationToken cancelacion)
        => _hub.Groups.AddToGroupAsync(connectionId, SesionesHub.GrupoListadoSesiones, cancelacion);

    public Task SalirDeListadoAsync(string connectionId, CancellationToken cancelacion)
        => _hub.Groups.RemoveFromGroupAsync(connectionId, SesionesHub.GrupoListadoSesiones, cancelacion);

    public async Task UnirseASesionAsync(
        string connectionId, Guid? usuarioId, IReadOnlyCollection<string> roles,
        Guid sesionId, CancellationToken cancelacion)
    {
        await _hub.Groups.AddToGroupAsync(connectionId, SesionesHub.GrupoSesion(sesionId), cancelacion);
        if (roles.Contains(ContextoActorTiempoReal.RolOperador) || roles.Contains(ContextoActorTiempoReal.RolAdministrador))
            await _hub.Groups.AddToGroupAsync(connectionId, SesionesHub.GrupoOperadoresSesion(sesionId), cancelacion);
    }

    public async Task SalirDeSesionAsync(string connectionId, Guid sesionId, CancellationToken cancelacion)
    {
        await _hub.Groups.RemoveFromGroupAsync(connectionId, SesionesHub.GrupoSesion(sesionId), cancelacion);
        await _hub.Groups.RemoveFromGroupAsync(connectionId, SesionesHub.GrupoOperadoresSesion(sesionId), cancelacion);
    }

    public Task UnirseAEquipoAsync(
        string connectionId, Guid? usuarioId, IReadOnlyCollection<string> roles,
        Guid equipoId, CancellationToken cancelacion)
        => _hub.Groups.AddToGroupAsync(connectionId, SesionesHub.GrupoEquipo(equipoId), cancelacion);

    public Task SalirDeEquipoAsync(string connectionId, Guid equipoId, CancellationToken cancelacion)
        => _hub.Groups.RemoveFromGroupAsync(connectionId, SesionesHub.GrupoEquipo(equipoId), cancelacion);
}
