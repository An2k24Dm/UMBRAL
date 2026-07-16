using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JuegosServicio.PruebasIntegracion;

public sealed class AuthHandlerPruebas : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Esquema = "PruebasAuth";
    public const string CabeceraRol = "X-Rol-Prueba";
    public const string CabeceraUsuarioId = "X-UsuarioId-Prueba";

    public AuthHandlerPruebas(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(CabeceraRol, out var rolHeader))
            return Task.FromResult(AuthenticateResult.NoResult());

        var rol = rolHeader.ToString();
        var usuarioId = Request.Headers.TryGetValue(CabeceraUsuarioId, out var idHeader)
            ? idHeader.ToString()
            : "11111111-1111-1111-1111-111111111111";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuarioId),
            new Claim(ClaimTypes.Name, usuarioId),
            new Claim(ClaimTypes.Role, rol),
            new Claim("roles", rol)
        };
        var identidad = new ClaimsIdentity(claims, Esquema, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identidad);
        var ticket = new AuthenticationTicket(principal, Esquema);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
