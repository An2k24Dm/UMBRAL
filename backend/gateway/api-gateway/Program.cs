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
// HU44 — Necesario para que el proxy reenvíe el handshake WebSocket de
// SignalR (/hubs/sesiones). Debe ejecutarse antes de MapReverseProxy.
aplicacion.UseWebSockets();
aplicacion.MapGet("/salud", () => Results.Ok(new { estado = "ok", servicio = "api-gateway" }));
aplicacion.MapReverseProxy();

await aplicacion.RunAsync();
