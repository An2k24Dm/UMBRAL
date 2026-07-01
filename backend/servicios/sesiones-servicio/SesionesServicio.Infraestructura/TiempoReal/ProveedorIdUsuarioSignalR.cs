using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace SesionesServicio.Infraestructura.TiempoReal;

// HU44 — Resuelve el "UserIdentifier" de cada conexión SignalR con el mismo
// identificador que IUsuarioActual.ObtenerId() (el sub del JWT, mapeado a
// NameIdentifier). Lo normaliza a Guid en formato "D" para que coincida
// exactamente con participanteIdentidadId.ToString() al usar Clients.User(...).
public sealed class ProveedorIdUsuarioSignalR : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection)
    {
        var sub = connection.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? connection.User?.FindFirstValue("sub");

        if (string.IsNullOrWhiteSpace(sub)) return null;

        return Guid.TryParse(sub, out var id) ? id.ToString() : sub;
    }
}
