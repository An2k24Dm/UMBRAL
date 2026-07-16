using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Infraestructura.Persistencia;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace JuegosServicio.PruebasIntegracion;

public sealed class FabricaApiPruebas : WebApplicationFactory<Program>
{
    public Mock<IClienteSesiones> MockClienteSesiones { get; } = new();

    private readonly string _nombreBaseDatos = "UmbralJuegosPruebas-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment("Testing");

        constructor.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>(
                    "ConnectionStrings:BaseDatos",
                    "Host=localhost;Database=umbral_juegos_tests;Username=u;Password=p"),
                new KeyValuePair<string, string?>(
                    "Keycloak:Authority", "http://localhost/realms/umbral"),
                new KeyValuePair<string, string?>(
                    "Cors:OrigenesPermitidos:0", "http://localhost:3000"),
                new KeyValuePair<string, string?>(
                    "ServiciosExternos:SesionesServicio:Url", "http://localhost")
            });
        });

        constructor.ConfigureServices(servicios =>
        {
            var aRemover = servicios.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ContextoJuegos>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ContextoJuegos) ||
                (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) ?? false) ||
                (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.Ordinal) ?? false))
                .ToList();
            foreach (var d in aRemover) servicios.Remove(d);

            var providerInMemory = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            servicios.AddDbContext<ContextoJuegos>(opciones =>
            {
                opciones.UseInMemoryDatabase(_nombreBaseDatos);
                opciones.UseInternalServiceProvider(providerInMemory);
            });

            MockClienteSesiones
                .Setup(c => c.ExisteSesionVigentePorMisionAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            QuitarYReemplazar<IClienteSesiones>(servicios, MockClienteSesiones.Object);

            servicios.AddAuthentication(opciones =>
            {
                opciones.DefaultAuthenticateScheme = AuthHandlerPruebas.Esquema;
                opciones.DefaultChallengeScheme = AuthHandlerPruebas.Esquema;
                opciones.DefaultScheme = AuthHandlerPruebas.Esquema;
            })
            .AddScheme<AuthenticationSchemeOptions, AuthHandlerPruebas>(
                AuthHandlerPruebas.Esquema, _ => { });

            using var alcance = servicios.BuildServiceProvider().CreateScope();
            var ctx = alcance.ServiceProvider.GetRequiredService<ContextoJuegos>();
            ctx.Database.EnsureCreated();
        });
    }

    private static void QuitarYReemplazar<TServicio>(
        IServiceCollection servicios, object implementacion)
        where TServicio : class
    {
        var existente = servicios.SingleOrDefault(d => d.ServiceType == typeof(TServicio));
        if (existente is not null) servicios.Remove(existente);
        servicios.AddSingleton(typeof(TServicio), implementacion);
    }
}
