using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

// Levanta una instancia de la API con base de datos InMemory y dobles
// para el cliente HTTP hacia juegos-servicio. Permite cubrir el flujo
// extremo a extremo (controlador → MediatR → repositorio) sin depender
// de PostgreSQL ni de un juegos-servicio real.
public sealed class FabricaApiPruebas : WebApplicationFactory<Program>
{
    public Mock<IClienteJuegosMisiones> MockClienteMisiones { get; } = new();
    public Mock<IGeneradorCodigoAcceso> MockGenerador { get; } = new();
    public Mock<IClienteIdentidadParticipantes> MockClienteIdentidadParticipantes { get; } = new();

    public static readonly Guid IdOperadorPrueba = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid IdOtroOperador = Guid.Parse("99999999-9999-9999-9999-999999999999");
    public static readonly Guid IdMisionActiva = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid IdMisionActivaB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid IdEtapaMisionActiva = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    public static readonly Guid IdTriviaMisionActiva = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
    public static readonly Guid IdMisionInactiva = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid IdMisionInexistente = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    public const string CodigoAccesoPrueba = "TEST01";

    private readonly string _nombreBaseDatos = "UmbralSesionesPruebas-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment("Testing");

        constructor.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new[]
            {
                new KeyValuePair<string, string?>(
                    "ConnectionStrings:BaseDatos",
                    "Host=localhost;Database=umbral_sesiones_tests;Username=u;Password=p"),
                new KeyValuePair<string, string?>(
                    "Keycloak:Authority", "http://localhost/realms/umbral"),
                new KeyValuePair<string, string?>(
                    "ServiciosExternos:JuegosServicio:Url", "http://localhost"),
                new KeyValuePair<string, string?>(
                    "ServiciosExternos:IdentidadServicio:Url", "http://localhost"),
                new KeyValuePair<string, string?>(
                    "Cors:OrigenesPermitidos:0", "http://localhost:3000")
            });
        });

        constructor.ConfigureServices(servicios =>
        {
            var aRemover = servicios.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ContextoSesiones>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ContextoSesiones) ||
                (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) ?? false) ||
                (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.Ordinal) ?? false))
                .ToList();
            foreach (var d in aRemover) servicios.Remove(d);

            var providerInMemory = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            servicios.AddDbContext<ContextoSesiones>(opciones =>
            {
                opciones.UseInMemoryDatabase(_nombreBaseDatos);
                opciones.UseInternalServiceProvider(providerInMemory);
                // El proveedor InMemory no soporta transacciones reales; los
                // manejadores que usan EjecutarEnTransaccionAsync (p. ej. HU52)
                // requieren que BeginTransaction sea un no-op en lugar de lanzar.
                opciones.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            ConfigurarMockClienteMisiones();
            QuitarYReemplazar<IClienteJuegosMisiones>(servicios, MockClienteMisiones.Object);

            MockGenerador.Setup(g => g.Generar()).Returns(CodigoAccesoPrueba);
            QuitarYReemplazar<IGeneradorCodigoAcceso>(servicios, MockGenerador.Object);

            // HU43 — Resuelve participantes con datos genéricos para no depender
            // de identidad-servicio real en las pruebas de integración.
            MockClienteIdentidadParticipantes
                .Setup(c => c.ObtenerParticipantesPorIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IEnumerable<Guid> ids, CancellationToken _) =>
                    (IReadOnlyDictionary<Guid, ParticipanteIdentidadResumenDto>)
                    ids.Distinct().ToDictionary(
                        id => id,
                        id => new ParticipanteIdentidadResumenDto
                        {
                            Id = id,
                            Nombre = "Nombre",
                            Apellido = "Apellido",
                            Alias = "alias-" + id.ToString("N")[..4]
                        }));
            QuitarYReemplazar<IClienteIdentidadParticipantes>(
                servicios, MockClienteIdentidadParticipantes.Object);

            var hosted = servicios
                .Where(d => d.ImplementationType?.Name == "ServicioPreparacionSesionesProgramadas")
                .ToList();
            foreach (var d in hosted) servicios.Remove(d);

            servicios.AddAuthentication(opciones =>
            {
                opciones.DefaultAuthenticateScheme = AuthHandlerPruebas.Esquema;
                opciones.DefaultChallengeScheme = AuthHandlerPruebas.Esquema;
                opciones.DefaultScheme = AuthHandlerPruebas.Esquema;
            })
            .AddScheme<AuthenticationSchemeOptions, AuthHandlerPruebas>(
                AuthHandlerPruebas.Esquema, _ => { });

            using var alcance = servicios.BuildServiceProvider().CreateScope();
            var ctx = alcance.ServiceProvider.GetRequiredService<ContextoSesiones>();
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

    private void ConfigurarMockClienteMisiones()
    {
        MockClienteMisiones
            .Setup(c => c.ObtenerMisionAsync(IdMisionActiva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MisionResumenJuegosDto
            {
                Id = IdMisionActiva, Nombre = "Misión Alfa",
                Estado = "Activa", TotalEtapas = 2, TiempoTotalSegundos = 120
            });

        MockClienteMisiones
            .Setup(c => c.ObtenerMisionAsync(IdMisionActivaB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MisionResumenJuegosDto
            {
                Id = IdMisionActivaB, Nombre = "Misión Bravo",
                Estado = "Activa", TotalEtapas = 1, TiempoTotalSegundos = 60
            });

        MockClienteMisiones
            .Setup(c => c.ObtenerMisionAsync(IdMisionInactiva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MisionResumenJuegosDto
            {
                Id = IdMisionInactiva, Nombre = "Misión Inactiva",
                Estado = "Inactiva", TotalEtapas = 2, TiempoTotalSegundos = 60
            });

        MockClienteMisiones
            .Setup(c => c.ObtenerMisionAsync(IdMisionInexistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MisionResumenJuegosDto?)null);

        MockClienteMisiones
            .Setup(c => c.ObtenerMisionConEtapasAsync(IdMisionActiva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MisionConEtapasJuegosDto
            {
                Id = IdMisionActiva,
                Nombre = "Mision Alfa",
                Estado = "Activa",
                Etapas =
                {
                    new EtapaJuegosDto
                    {
                        Id = IdEtapaMisionActiva,
                        Orden = 1,
                        TipoModoDeJuego = "Trivia",
                        ModoDeJuegoId = IdTriviaMisionActiva,
                        NombreModoDeJuego = "Trivia Alfa",
                        TiempoEstimado = 120
                    }
                }
            });

        MockClienteMisiones
            .Setup(c => c.ObtenerMisionConEtapasAsync(IdMisionActivaB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MisionConEtapasJuegosDto
            {
                Id = IdMisionActivaB,
                Nombre = "Mision Bravo",
                Estado = "Activa",
                Etapas =
                {
                    new EtapaJuegosDto
                    {
                        Id = Guid.Parse("abababab-abab-abab-abab-abababababab"),
                        Orden = 1,
                        TipoModoDeJuego = "Trivia",
                        ModoDeJuegoId = Guid.Parse("cdcdcdcd-cdcd-cdcd-cdcd-cdcdcdcdcdcd"),
                        NombreModoDeJuego = "Trivia Bravo",
                        TiempoEstimado = 60
                    }
                }
            });
    }
}
