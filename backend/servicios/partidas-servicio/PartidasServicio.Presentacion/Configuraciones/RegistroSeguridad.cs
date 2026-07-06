using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace PartidasServicio.Presentacion.Configuraciones;

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
                    OnMessageReceived = ctx =>
                    {
                        var accessToken = ctx.Request.Query["access_token"];
                        if (!string.IsNullOrEmpty(accessToken) &&
                            ctx.HttpContext.Request.Path.StartsWithSegments("/hubs/partidas"))
                            ctx.Token = accessToken;
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ctx =>
                    {
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
                                            identidad.AddClaim(
                                                new System.Security.Claims.Claim("roles", rol));
                                    }
                                }
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnForbidden = async ctx =>
                    {
                        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            codigo = "ACCESO_DENEGADO",
                            mensaje = "No tienes permisos para realizar esta acción."
                        }));
                    },
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        ctx.Response.ContentType = "application/json";
                        await ctx.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            codigo = "NO_AUTENTICADO",
                            mensaje = "Debe iniciar sesión para realizar esta acción."
                        }));
                    }
                };
            });

        servicios.AddAuthorization(opcAuth =>
        {
            opcAuth.AddPolicy("PoliticaSoloParticipante",
                p => p.RequireAuthenticatedUser().RequireRole("Participante"));
            opcAuth.AddPolicy("PoliticaAdministradorUOperador",
                p => p.RequireAuthenticatedUser().RequireRole("Administrador", "Operador"));
            opcAuth.AddPolicy("PoliticaAutenticado",
                p => p.RequireAuthenticatedUser());
        });

        return servicios;
    }
}
