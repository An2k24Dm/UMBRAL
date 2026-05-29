using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace SesionesServicio.Api.Configuraciones;

public static class RegistroSeguridad
{
    public static IServiceCollection AgregarSeguridad(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var opciones = new OpcionesKeycloak();
        configuracion.GetSection(OpcionesKeycloak.Seccion).Bind(opciones);

        servicios.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(parametros =>
            {
                parametros.Authority = opciones.Authority;
                parametros.RequireHttpsMetadata = false;
                parametros.MetadataAddress = opciones.MetadataAddress;
                parametros.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = opciones.Authority,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    RoleClaimType = "roles",
                    NameClaimType = "preferred_username",
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
                parametros.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        // Aplana realm_access.roles a claims 'roles' para
                        // que [Authorize(Roles=...)] y IUsuarioActual.Roles
                        // funcionen.
                        if (ctx.Principal?.Identity is System.Security.Claims.ClaimsIdentity identidad)
                        {
                            var realm = ctx.Principal.FindFirst("realm_access")?.Value;
                            if (!string.IsNullOrWhiteSpace(realm))
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(realm);
                                if (doc.RootElement.TryGetProperty("roles", out var roles) &&
                                    roles.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var item in roles.EnumerateArray())
                                    {
                                        var rol = item.GetString();
                                        if (!string.IsNullOrWhiteSpace(rol))
                                            identidad.AddClaim(new System.Security.Claims.Claim("roles", rol));
                                    }
                                }
                            }
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        servicios.AddAuthorization(opcAuth =>
        {
            opcAuth.AddPolicy("PoliticaAdministradorUOperador",
                p => p.RequireRole("Administrador", "Operador"));
        });

        return servicios;
    }
}
