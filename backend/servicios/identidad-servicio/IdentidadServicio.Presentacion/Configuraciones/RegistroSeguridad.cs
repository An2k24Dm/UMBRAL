using IdentidadServicio.Infraestructura.Seguridad;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace IdentidadServicio.Presentacion.Configuraciones;

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
                        // Aplana realm_access.roles a claims 'roles' para [Authorize(Roles=...)]
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
            opcAuth.AddPolicy("PoliticaAdministrador", p => p.RequireRole("Administrador"));
            opcAuth.AddPolicy("PoliticaOperador", p => p.RequireRole("Operador"));
            opcAuth.AddPolicy("PoliticaParticipante", p => p.RequireRole("Participante"));
            // HU07: consulta de Participantes desde el panel web. Permite a
            // Administrador y Operador. Participante no puede acceder al panel.
            opcAuth.AddPolicy("PoliticaAdministradorUOperador",
                p => p.RequireRole("Administrador", "Operador"));
            // HU43 — Datos básicos de participantes por ids. Lo consume
            // sesiones-servicio reenviando el token del Operador o Participante.
            opcAuth.AddPolicy("PoliticaConsultaParticipantesBasicos",
                p => p.RequireRole("Administrador", "Operador", "Participante"));
        });

        return servicios;
    }
}
