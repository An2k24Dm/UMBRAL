using IdentidadServicio.Presentacion.Configuraciones;
using IdentidadServicio.Presentacion.Middlewares;
using IdentidadServicio.Aplicacion.Dependencias;
using IdentidadServicio.Infraestructura.Dependencias;
using IdentidadServicio.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

var constructor = WebApplication.CreateBuilder(args);

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

aplicacion.UseMiddleware<ManejadorErroresMiddleware>();

if (aplicacion.Environment.IsDevelopment())
{
    aplicacion.UseSwagger();
    aplicacion.UseSwaggerUI();
}

aplicacion.UseCors(RegistroCors.PoliticaUmbral);
aplicacion.UseAuthentication();
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