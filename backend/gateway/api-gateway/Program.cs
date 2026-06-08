var constructor = WebApplication.CreateBuilder(args);

constructor.Services.AddReverseProxy()
    .LoadFromConfig(constructor.Configuration.GetSection("ReverseProxy"));

constructor.Services.AddCors(opciones =>
{
    opciones.AddDefaultPolicy(p => p
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var aplicacion = constructor.Build();

aplicacion.UseCors();
aplicacion.MapGet("/salud", () => Results.Ok(new { estado = "ok", servicio = "api-gateway" }));
aplicacion.MapReverseProxy();

await aplicacion.RunAsync();
