using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PartidasServicio.Aplicacion.Dependencias;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Infraestructura.Dependencias;
using PartidasServicio.Infraestructura.Persistencia;
using PartidasServicio.Infraestructura.TiempoReal;
using PartidasServicio.Infraestructura.TiempoReal.Hubs;
using PartidasServicio.Presentacion.Configuraciones;
using PartidasServicio.Presentacion.Middlewares;

var constructor = WebApplication.CreateBuilder(args);

constructor.Logging.ClearProviders();
constructor.Logging.AddSimpleConsole(opciones =>
{
    opciones.SingleLine = true;
    opciones.TimestampFormat = "HH:mm:ss ";
    opciones.IncludeScopes = true;
});
constructor.Logging.AddDebug();

constructor.Services.AddControllers().AddJsonOptions(opciones =>
{
    opciones.JsonSerializerOptions.Converters
        .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

constructor.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(opciones =>
{
    opciones.InvalidModelStateResponseFactory = contexto =>
        RespuestaErrorModelo.ConstruirDesdeModelState(contexto.ModelState);
});

constructor.Services.AddEndpointsApiExplorer();
constructor.Services.AddSwaggerGen();

constructor.Services.AddHttpContextAccessor();
constructor.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();
constructor.Services.AddScoped<IPropagadorTokenActual, PropagadorTokenActualHttp>();
constructor.Services.AddSignalR();

constructor.Services.AgregarAplicacion();
constructor.Services.AgregarInfraestructura(constructor.Configuration);
constructor.Services.AgregarSeguridad(constructor.Configuration);
constructor.Services.AgregarCorsUmbral(constructor.Configuration);

var aplicacion = constructor.Build();

if (aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseSwagger();
    aplicacion.UseSwaggerUI();
}

aplicacion.UseCors(RegistroCors.PoliticaUmbral);
aplicacion.UseAuthentication();
aplicacion.UseMiddleware<LoggingSolicitudesMiddleware>();
aplicacion.UseMiddleware<ManejadorErroresMiddleware>();
aplicacion.UseAuthorization();

aplicacion.MapControllers();
aplicacion.MapHub<PartidasHub>("/hubs/partidas");

aplicacion.MapGet("/salud", () => Results.Ok(new { estado = "ok", servicio = "partidas-servicio" }));

if (!aplicacion.Environment.IsEnvironment("Testing"))
{
    using var alcance = aplicacion.Services.CreateScope();
    var contexto = alcance.ServiceProvider.GetRequiredService<ContextoPartidas>();
    await contexto.Database.MigrateAsync();
}

await aplicacion.RunAsync();

public partial class Program { }
