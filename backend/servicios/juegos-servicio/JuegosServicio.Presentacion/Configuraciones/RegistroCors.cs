using Microsoft.Extensions.Options;

namespace JuegosServicio.Presentacion.Configuraciones;

public static class RegistroCors
{
    public const string PoliticaUmbral = "PoliticaUmbral";

    public static IServiceCollection AgregarCorsUmbral(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var origenes = configuracion
            .GetSection("Cors:OrigenesPermitidos")
            .Get<string[]>() ?? Array.Empty<string>();

        servicios.AddCors(opciones =>
        {
            opciones.AddPolicy(PoliticaUmbral, constructor =>
            {
                if (origenes.Length == 0)
                    constructor.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                else
                    constructor.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod();
            });
        });

        return servicios;
    }
}
