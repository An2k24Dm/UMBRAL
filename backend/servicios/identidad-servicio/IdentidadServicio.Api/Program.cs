using IdentidadServicio.Api.Configuraciones;
using IdentidadServicio.Api.Middlewares;
using IdentidadServicio.Aplicacion.Dependencias;
using IdentidadServicio.Infraestructura.Dependencias;
using IdentidadServicio.Infraestructura.Persistencia;
using Microsoft.EntityFrameworkCore;

var constructor = WebApplication.CreateBuilder(args);

// Acepta enums como string en JSON (p. ej. "TipoUsuario": "Operador",
// "Sexo": "Masculino"). Sin esto el frontend debería enviar el valor numérico.
constructor.Services.AddControllers().AddJsonOptions(opciones =>
{
    opciones.JsonSerializerOptions.Converters
        .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});

constructor.Services.AddEndpointsApiExplorer();
constructor.Services.AddSwaggerGen();

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