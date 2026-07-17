using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Infraestructura.Persistencia;
using RankingServicio.Infraestructura.RabbitMq;

namespace RankingServicio.PruebasIntegracion;

// Levanta ranking-servicio en memoria (Testing) para pruebas de integración:
// - EF Core InMemory en lugar de Npgsql.
// - Sin los BackgroundService de RabbitMQ (consumidor/outbox), que requieren un
//   broker real; se quitan para no intentar conexiones externas.
// - Clientes HTTP hacia identidad/sesiones reemplazados por mocks (el
//   enriquecimiento de alias/nombres no debe salir a la red).
// - Autenticación JWT/Keycloak reemplazada por un esquema de cabecera.
public sealed class FabricaApiRankingPruebas : WebApplicationFactory<Program>
{
    // SQLite en memoria: proveedor relacional real (soporta AsSplitQuery, Include
    // y conversiones de valor). La conexión se mantiene abierta durante la vida de
    // la fábrica para conservar los datos entre contextos.
    private readonly SqliteConnection _conexion = new("DataSource=:memory:");

    public Mock<IClienteIdentidadParticipantes> MockClienteIdentidad { get; } = new();
    public Mock<IClienteSesionesRanking> MockClienteSesiones { get; } = new();

    // Datos sembrados (una sesión con ranking; participantes y dos equipos).
    public static readonly Guid SesionConDatos = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid SesionSinRanking = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid IdentidadA = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    public static readonly Guid IdentidadB = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000002");
    public static readonly Guid IdentidadC = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000003");
    public static readonly Guid IdentidadD = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000004");
    public static readonly Guid Equipo1 = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000001");
    public static readonly Guid Equipo2 = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    public const string AliasA = "aa";
    public const string NombreEquipo1 = "Equipo Uno";

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        _conexion.Open();
        constructor.UseEnvironment("Testing");

        constructor.ConfigureServices(servicios =>
        {
            // 1) Quitar Npgsql y su DbContext.
            var aRemover = servicios.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ContextoRanking>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ContextoRanking) ||
                (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) ?? false) ||
                (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.Ordinal) ?? false))
                .ToList();
            foreach (var d in aRemover) servicios.Remove(d);

            // Proveedor EF aislado (evita conflicto con el Npgsql ya registrado).
            var providerSqlite = new ServiceCollection()
                .AddEntityFrameworkSqlite()
                .BuildServiceProvider();

            servicios.AddDbContext<ContextoRanking>(opciones =>
            {
                opciones.UseSqlite(_conexion);
                opciones.UseInternalServiceProvider(providerSqlite);
            });

            // 2) Quitar los BackgroundService de RabbitMQ (requieren broker real).
            var hostedARemover = servicios.Where(d =>
                d.ServiceType == typeof(IHostedService) &&
                (d.ImplementationType == typeof(ConsumidorEventosRanking) ||
                 d.ImplementationType == typeof(DespachadorOutboxResultadosRabbitMq)))
                .ToList();
            foreach (var d in hostedARemover) servicios.Remove(d);

            // 3) Reemplazar clientes HTTP de enriquecimiento por mocks.
            foreach (var tipo in new[] { typeof(IClienteIdentidadParticipantes), typeof(IClienteSesionesRanking) })
            {
                foreach (var d in servicios.Where(x => x.ServiceType == tipo).ToList())
                    servicios.Remove(d);
            }

            MockClienteIdentidad
                .Setup(c => c.ObtenerParticipantesPorIdsAsync(
                    It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Guid, ParticipanteIdentidadResumenDto>
                {
                    [IdentidadA] = new() { Id = IdentidadA, Alias = AliasA, Nombre = "Ana", Apellido = "A" }
                    // B, C y D no vienen: ejercitan el fallback de ResolucionAlias.
                });
            MockClienteSesiones
                .Setup(c => c.ObtenerNombresEquiposAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Dictionary<Guid, string>
                {
                    [Equipo1] = NombreEquipo1
                    // Equipo2 no viene: ejercita el fallback al id del equipo.
                });

            servicios.AddSingleton(MockClienteIdentidad.Object);
            servicios.AddSingleton(MockClienteSesiones.Object);

            // 4) Autenticación de prueba dirigida por cabecera.
            servicios.AddAuthentication(opciones =>
            {
                opciones.DefaultAuthenticateScheme = AuthHandlerPruebas.Esquema;
                opciones.DefaultChallengeScheme = AuthHandlerPruebas.Esquema;
                opciones.DefaultScheme = AuthHandlerPruebas.Esquema;
            })
            .AddScheme<AuthenticationSchemeOptions, AuthHandlerPruebas>(
                AuthHandlerPruebas.Esquema, _ => { });

            // 5) Crear y sembrar la base InMemory.
            using var alcance = servicios.BuildServiceProvider().CreateScope();
            var contexto = alcance.ServiceProvider.GetRequiredService<ContextoRanking>();
            contexto.Database.EnsureCreated();
            if (!contexto.Rankings.Any()) Sembrar(contexto);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) _conexion.Dispose();
    }

    private static void Sembrar(ContextoRanking contexto)
    {
        var ranking = Ranking.Crear(SesionConDatos);
        // Equipo1: A (100) + B (50) = 150 ; C sin equipo (30) ; Equipo2: D (40).
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), IdentidadA, Equipo1, 100);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), IdentidadB, Equipo1, 50);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), IdentidadC, null, 30);
        ranking.RegistrarPuntajeParticipante(Guid.NewGuid(), IdentidadD, Equipo2, 40);

        contexto.Rankings.Add(ranking);
        contexto.SaveChanges();
    }
}
