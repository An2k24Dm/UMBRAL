using IdentidadServicio.Presentacion.Configuraciones;
using IdentidadServicio.Presentacion.Middlewares;
using IdentidadServicio.Aplicacion.Dependencias;
using IdentidadServicio.Infraestructura.Dependencias;
using IdentidadServicio.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

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
        IdentidadServicio.Presentacion.Configuraciones.RespuestaErrorModelo
            .ConstruirDesdeModelState(contexto.ModelState);
});

constructor.Services.AddEndpointsApiExplorer();
constructor.Services.AddSwaggerGen();
constructor.Services.AddHttpContextAccessor();
constructor.Services.AddScoped<
    IdentidadServicio.Aplicacion.Puertos.IUsuarioActual,
    IdentidadServicio.Presentacion.Configuraciones.UsuarioActualHttp>();

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
aplicacion.UseMiddleware<BloqueoUsuarioInactivoMiddleware>();
aplicacion.UseAuthorization();

aplicacion.MapControllers();

aplicacion.MapGet("/salud", () => Results.Ok(new { estado = "ok" }));

if (!aplicacion.Environment.IsEnvironment("Testing"))
{
    using var alcance = aplicacion.Services.CreateScope();

    var contexto = alcance.ServiceProvider.GetRequiredService<ContextoIdentidad>();

    var reloj = alcance.ServiceProvider
        .GetRequiredService<IdentidadServicio.Aplicacion.Puertos.IProveedorFechaHora>();

    await contexto.Database.MigrateAsync();

    await SembradorIdentidad.SembrarAsync(contexto, reloj, CancellationToken.None);
}

await aplicacion.RunAsync();

public partial class Program { }
