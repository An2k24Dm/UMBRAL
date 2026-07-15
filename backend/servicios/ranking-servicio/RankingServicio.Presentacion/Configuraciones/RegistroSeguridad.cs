using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace RankingServicio.Presentacion.Configuraciones;

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
                        var path = ctx.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/hubs/ranking"))
                        {
                            ctx.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ctx =>
                    {
                        if (ctx.Principal?.Identity is System.Security.Claims.ClaimsIdentity id)
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
                                            id.AddClaim(new System.Security.Claims.Claim("roles", rol));
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

        servicios.AddAuthorization(opc =>
        {
            opc.AddPolicy("PoliticaAutenticado",
                p => p.RequireAuthenticatedUser());
            opc.AddPolicy("PoliticaAdministradorUOperador",
                p => p.RequireAuthenticatedUser().RequireRole("Administrador", "Operador"));
            opc.AddPolicy("PoliticaParticipante",
                p => p.RequireAuthenticatedUser().RequireRole("Participante"));
        });

        return servicios;
    }
}
