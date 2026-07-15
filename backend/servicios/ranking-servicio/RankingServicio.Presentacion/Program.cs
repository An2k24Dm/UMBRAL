using Microsoft.EntityFrameworkCore;
using RankingServicio.Aplicacion.Dependencias;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Infraestructura.Dependencias;
using RankingServicio.Infraestructura.Persistencia;
using RankingServicio.Infraestructura.TiempoReal;
using RankingServicio.Presentacion.Configuraciones;
using RankingServicio.Presentacion.Middlewares;

var constructor = WebApplication.CreateBuilder(args);

constructor.Logging.ClearProviders();
constructor.Logging.AddSimpleConsole(opciones =>
{
    opciones.SingleLine = true;
    opciones.TimestampFormat = "HH:mm:ss ";
    // IncludeScopes conserva CorrelationId y Servicio (BeginScope del middleware).
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

constructor.Services.AddSignalR(opciones =>
{
    opciones.KeepAliveInterval = TimeSpan.FromSeconds(15);
    opciones.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// Necesario para que los clientes HTTP de enriquecimiento reenvíen el token
// Bearer del usuario actual hacia identidad/sesiones.
constructor.Services.AddHttpContextAccessor();
constructor.Services.AddScoped<IPropagadorTokenActual, PropagadorTokenActualHttp>();

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
aplicacion.MapHub<RankingHub>("/hubs/ranking");

aplicacion.MapGet("/salud", () => Results.Ok(new { estado = "ok", servicio = "ranking-servicio" }));

if (!aplicacion.Environment.IsEnvironment("Testing"))
{
    using var alcance = aplicacion.Services.CreateScope();
    var contexto = alcance.ServiceProvider.GetRequiredService<ContextoRanking>();
    await contexto.Database.MigrateAsync();
}

await aplicacion.RunAsync();

public partial class Program { }
