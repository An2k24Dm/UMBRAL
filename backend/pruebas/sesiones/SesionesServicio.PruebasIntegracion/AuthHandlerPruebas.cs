using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SesionesServicio.PruebasIntegracion;

// Esquema de autenticación de pruebas: lee "X-Rol-Prueba" del request y,
// si está presente, materializa un ClaimsPrincipal con el rol indicado.
// Mantiene el mismo contrato que el AuthHandlerPruebas de identidad
// para que las pruebas de integración no necesiten Keycloak.
public sealed class AuthHandlerPruebas : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Esquema = "PruebasAuth";
    public const string CabeceraRol = "X-Rol-Prueba";
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
            return Task.FromResult(AuthenticateResult.NoResult());

        var rol = rolHeader.ToString();
        var idKeycloak = Request.Headers.TryGetValue(CabeceraIdKeycloak, out var subHeader)
            ? subHeader.ToString()
            : "11111111-1111-1111-1111-111111111111";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, idKeycloak),
            new Claim(ClaimTypes.Name, idKeycloak),
            new Claim("preferred_username", idKeycloak),
            new Claim(ClaimTypes.Role, rol),
            new Claim("roles", rol)
        };
        var identidad = new ClaimsIdentity(claims, Esquema, ClaimTypes.Name, ClaimTypes.Role);
        var principal = new ClaimsPrincipal(identidad);
        var ticket = new AuthenticationTicket(principal, Esquema);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
