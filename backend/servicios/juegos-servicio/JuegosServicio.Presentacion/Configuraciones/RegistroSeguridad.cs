namespace JuegosServicio.Presentacion.Configuraciones;

public static class RegistroSeguridad
{
    public static IServiceCollection AgregarSeguridadJuegos(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var authority = configuracion["Keycloak:Authority"]
            ?? throw new InvalidOperationException("Falta la configuración 'Keycloak:Authority'.");

        servicios.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer", opciones =>
            {
                opciones.Authority = authority;
                opciones.RequireHttpsMetadata = false;
                opciones.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateAudience = false,
                    RoleClaimType = "roles"
                };
                opciones.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
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
                                            identidad.AddClaim(new System.Security.Claims.Claim("roles", rol));
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
                        var cuerpo = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            codigo = "ACCESO_DENEGADO",
                            mensaje = "No tienes permisos para realizar esta acción."
                        });
                        await ctx.Response.WriteAsync(cuerpo);
                    },
                    OnChallenge = async ctx =>
                    {
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        ctx.Response.ContentType = "application/json";
                        var cuerpo = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            codigo = "NO_AUTENTICADO",
                            mensaje = "Debe iniciar sesión para realizar esta acción."
                        });
                        await ctx.Response.WriteAsync(cuerpo);
                    }
                };
            });

        servicios.AddAuthorization(opciones =>
        {
            opciones.AddPolicy("PoliticaOperador", p =>
                p.RequireAuthenticatedUser()
                 .RequireRole("Operador", "Administrador"));

            opciones.AddPolicy("PoliticaAdministrador", p =>
                p.RequireAuthenticatedUser()
                 .RequireRole("Administrador"));

            // Flujo móvil del Participante: necesita poder consultar el
            // detalle de una misión asociada a una sesión disponible.
            // Esta política habilita SOLO consultas de lectura; los
            // endpoints administrativos siguen exigiendo PoliticaOperador
            // o PoliticaAdministrador.
            opciones.AddPolicy("PoliticaSoloParticipante", p =>
                p.RequireAuthenticatedUser()
                 .RequireRole("Participante"));
        });

        return servicios;
    }
}
