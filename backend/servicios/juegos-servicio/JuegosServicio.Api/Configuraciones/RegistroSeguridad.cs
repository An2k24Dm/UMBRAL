namespace JuegosServicio.Api.Configuraciones;

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
                    ValidateAudience = false
                };
            });

        servicios.AddAuthorization(opciones =>
        {
            opciones.AddPolicy("PoliticaOperador", p =>
                p.RequireAuthenticatedUser()
                 .RequireRole("Operador", "Administrador"));
        });

        return servicios;
    }
}
