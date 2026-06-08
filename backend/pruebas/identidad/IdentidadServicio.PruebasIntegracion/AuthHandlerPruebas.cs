using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdentidadServicio.PruebasIntegracion;

// Esquema de autenticación para pruebas de integración.
// Lee la cabecera "X-Rol-Prueba" y, si está presente, construye un ClaimsPrincipal
// con el rol indicado. Permite verificar 401/403/201 sin levantar Keycloak real.
public sealed class AuthHandlerPruebas : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Esquema = "PruebasAuth";
    public const string CabeceraRol = "X-Rol-Prueba";
    // HU10 — permite que la prueba de integración fije el sub (IdKeycloak)
    // emitido por el token. Si no se envía, se usa "tester" como antes para
    // preservar el comportamiento de las pruebas previas a HU10.
    public const string CabeceraIdKeycloak = "X-IdKeycloak-Prueba";

    public AuthHandlerPruebas(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(CabeceraRol, out var rolHeader))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var rol = rolHeader.ToString();
        var idKeycloak = Request.Headers.TryGetValue(CabeceraIdKeycloak, out var subHeader)
            ? subHeader.ToString()
            : "tester";
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, idKeycloak),
            new Claim(ClaimTypes.Name, idKeycloak),
            new Claim(ClaimTypes.Role, rol),
            new Claim("roles", rol)
        };
        var identidad = new ClaimsIdentity(claims, Esquema, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identidad);
        var ticket = new AuthenticationTicket(principal, Esquema);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
