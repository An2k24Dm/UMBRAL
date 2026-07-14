var constructor = WebApplication.CreateBuilder(args);

constructor.Services.AddReverseProxy()
    .LoadFromConfig(constructor.Configuration.GetSection("ReverseProxy"));

var aplicacion = constructor.Build();

// CORS lo maneja cada servicio backend (AllowCredentials + orígenes específicos).
// El gateway solo hace proxy — UseCors con AllowAnyOrigin bloquearía SignalR.
aplicacion.UseWebSockets();
aplicacion.MapGet("/salud", () => Results.Ok(new { estado = "ok", servicio = "api-gateway" }));
aplicacion.MapReverseProxy();

await aplicacion.RunAsync();
