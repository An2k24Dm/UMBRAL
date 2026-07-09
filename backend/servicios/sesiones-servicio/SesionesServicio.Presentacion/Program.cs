using Microsoft.EntityFrameworkCore;
using SesionesServicio.Presentacion.Configuraciones;
using SesionesServicio.Presentacion.Middlewares;
using SesionesServicio.Aplicacion.Dependencias;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Infraestructura.Dependencias;
using Microsoft.AspNetCore.SignalR;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.TiempoReal;
using SesionesServicio.Infraestructura.TiempoReal.Hubs;

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
        SesionesServicio.Presentacion.Configuraciones.RespuestaErrorModelo
            .ConstruirDesdeModelState(contexto.ModelState);
});

constructor.Services.AddEndpointsApiExplorer();
constructor.Services.AddSwaggerGen();

constructor.Services.AddHttpContextAccessor();
constructor.Services.AddScoped<IUsuarioActual, UsuarioActualHttp>();
constructor.Services.AddScoped<IPropagadorTokenActual, PropagadorTokenActualHttp>();
// HU44 — Mapea cada conexión SignalR al id de usuario del JWT para poder
// dirigir avisos de expulsión con Clients.User(...).
constructor.Services.AddSingleton<IUserIdProvider, ProveedorIdUsuarioSignalR>();
constructor.Services.AddSignalR(opciones =>
{
    opciones.KeepAliveInterval = TimeSpan.FromSeconds(15);
    opciones.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

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

// El logging va después de la autenticación para poder registrar el usuario y
// el rol del token, y envuelve al manejador de errores: mide el tiempo total
// del request y registra el código final, incluso cuando hubo una excepción
// que el manejador de errores tradujo a respuesta JSON.
aplicacion.UseMiddleware<LoggingSolicitudesMiddleware>();
aplicacion.UseMiddleware<ManejadorErroresMiddleware>();
aplicacion.UseAuthorization();

aplicacion.MapControllers();
aplicacion.MapHub<SesionesHub>("/hubs/sesiones");

aplicacion.MapGet("/salud", () => Results.Ok(new { estado = "ok", servicio = "sesiones-servicio" }));

if (!aplicacion.Environment.IsEnvironment("Testing"))
{
    using var alcance = aplicacion.Services.CreateScope();
    var contexto = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
    await contexto.Database.MigrateAsync();
}

await aplicacion.RunAsync();

public partial class Program { }
