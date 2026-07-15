using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace RankingServicio.Infraestructura.TiempoReal;

[Authorize]
public sealed class RankingHub : Hub
{
    private readonly ILogger<RankingHub> _logger;

    public RankingHub(ILogger<RankingHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "SignalR Ranking conectado. ConnectionId={ConnectionId} UsuarioId={UsuarioId}",
            Context.ConnectionId,
            ObtenerUsuarioId());
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is null)
        {
            _logger.LogInformation(
                "SignalR Ranking desconectado. ConnectionId={ConnectionId} UsuarioId={UsuarioId}",
                Context.ConnectionId,
                ObtenerUsuarioId());
        }
        else if (EsDesconexionTransitoria(exception))
        {
            _logger.LogWarning(
                "SignalR Ranking desconexión transitoria. ConnectionId={ConnectionId} UsuarioId={UsuarioId} TipoExcepcion={TipoExcepcion} Mensaje={Mensaje}",
                Context.ConnectionId,
                ObtenerUsuarioId(),
                exception.GetType().Name,
                exception.Message);
        }
        else
        {
            _logger.LogError(
                exception,
                "SignalR Ranking desconexión inesperada. ConnectionId={ConnectionId} UsuarioId={UsuarioId} TipoExcepcion={TipoExcepcion}",
                Context.ConnectionId,
                ObtenerUsuarioId(),
                exception.GetType().Name);
        }

        return base.OnDisconnectedAsync(exception);
    }

    public async Task UnirseASesion(string sesionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"sesion:{sesionId}");
        _logger.LogInformation(
            "SignalR Ranking unido a sesión. ConnectionId={ConnectionId} UsuarioId={UsuarioId} SesionId={SesionId}",
            Context.ConnectionId,
            ObtenerUsuarioId(),
            sesionId);
    }

    public async Task SalirDeSesion(string sesionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sesion:{sesionId}");
        _logger.LogDebug(
            "SignalR Ranking salió de sesión. ConnectionId={ConnectionId} SesionId={SesionId}",
            Context.ConnectionId,
            sesionId);
    }

    // Nunca se registra el JWT: solo el id de usuario del token.
    private string ObtenerUsuarioId()
        => Context.UserIdentifier
           ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? Context.User?.FindFirstValue("sub")
           ?? "Desconocido";

    // Cierres de transporte que la reconexión automática del cliente resuelve:
    // se registran como Warning, no como Error terminal. ConnectionAbortedException
    // deriva de OperationCanceledException, por lo que queda cubierta.
    private static bool EsDesconexionTransitoria(Exception exception)
        => exception is OperationCanceledException
            or System.IO.IOException
            or System.Net.WebSockets.WebSocketException;
}
