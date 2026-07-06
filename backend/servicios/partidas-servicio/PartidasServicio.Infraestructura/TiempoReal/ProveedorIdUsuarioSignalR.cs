using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace PartidasServicio.Infraestructura.TiempoReal;

public sealed class ProveedorIdUsuarioSignalR : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext conexion)
        => conexion.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
           ?? conexion.User?.FindFirst("sub")?.Value;
}
