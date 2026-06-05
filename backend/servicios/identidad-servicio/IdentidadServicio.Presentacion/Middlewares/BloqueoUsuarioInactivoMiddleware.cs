using System.Net;
using System.Security.Claims;
using System.Text.Json;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.Presentacion.Middlewares;

public sealed class BloqueoUsuarioInactivoMiddleware
{
    private static readonly JsonSerializerOptions OpcionesJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _siguiente;
    private readonly ILogger<BloqueoUsuarioInactivoMiddleware> _registro;

    public BloqueoUsuarioInactivoMiddleware(
        RequestDelegate siguiente,
        ILogger<BloqueoUsuarioInactivoMiddleware> registro)
    {
        _siguiente = siguiente;
        _registro = registro;
    }

    public async Task Invoke(
        HttpContext contexto,
        IRepositorioUsuariosLectura repositorioLectura)
    {
        var principal = contexto.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            var idKeycloak = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? principal.FindFirstValue("sub");
            if (!string.IsNullOrWhiteSpace(idKeycloak))
            {
                var usuario = await repositorioLectura.ObtenerPorIdKeycloakAsync(
                    idKeycloak, contexto.RequestAborted);
                if (usuario is not null && usuario.Estado != EstadoUsuario.Activo)
                {
                    _registro.LogWarning(
                        "Petición bloqueada: usuario {Id} ({Rol}) está Inactivo (HU12).",
                        usuario.Id, usuario.Rol);
                    await EscribirJsonAsync(contexto, HttpStatusCode.Forbidden, new
                    {
                        codigo = "CUENTA_DESACTIVADA",
                        mensaje = "La cuenta se encuentra desactivada."
                    });
                    return;
                }
            }
        }

        await _siguiente(contexto);
    }

    private static Task EscribirJsonAsync(
        HttpContext contexto, HttpStatusCode estado, object cuerpo)
    {
        contexto.Response.StatusCode = (int)estado;
        contexto.Response.ContentType = "application/json";
        return contexto.Response.WriteAsync(JsonSerializer.Serialize(cuerpo, OpcionesJson));
    }
}
