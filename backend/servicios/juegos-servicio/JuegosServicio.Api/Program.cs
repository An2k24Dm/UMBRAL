using JuegosServicio.Api.Configuraciones;
using JuegosServicio.Api.Middlewares;
using JuegosServicio.Aplicacion.Dependencias;
using JuegosServicio.Infraestructura.Dependencias;
using JuegosServicio.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

var constructor = WebApplication.CreateBuilder(args);

constructor.Services.AddControllers().AddJsonOptions(opciones =>
{
    opciones.JsonSerializerOptions.Converters
        .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

constructor.Services.AddEndpointsApiExplorer();
constructor.Services.AddSwaggerGen();

constructor.Services.AgregarAplicacion();
constructor.Services.AgregarInfraestructura(constructor.Configuration);
constructor.Services.AgregarSeguridadJuegos(constructor.Configuration);
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
aplicacion.UseAuthorization();

aplicacion.MapControllers();

aplicacion.MapGet("/salud", () => Results.Ok(new { estado = "ok", servicio = "juegos-servicio" }));

if (!aplicacion.Environment.IsEnvironment("Testing"))
{
    using var alcance = aplicacion.Services.CreateScope();
    var contexto = alcance.ServiceProvider.GetRequiredService<ContextoJuegos>();
    await contexto.Database.MigrateAsync();
    await SembradorJuegos.SembrarAsync(contexto, CancellationToken.None);
}

await aplicacion.RunAsync();

public partial class Program { }
