namespace IdentidadServicio.Presentacion.Configuraciones;

public static class RegistroCors
{
    public const string PoliticaUmbral = "PoliticaUmbral";

    public static IServiceCollection AgregarCorsUmbral(
        this IServiceCollection servicios, IConfiguration configuracion)
    {
        var origenes = configuracion.GetSection("Cors:OrigenesPermitidos").Get<string[]>()
                       ?? Array.Empty<string>();

        servicios.AddCors(opciones =>
        {
            opciones.AddPolicy(PoliticaUmbral, c =>
            {
                c.WithOrigins(origenes).AllowAnyHeader().AllowAnyMethod();
            });
        });

        return servicios;
    }
}
