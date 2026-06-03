using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia;

namespace SesionesServicio.PruebasIntegracion;

// Levanta una instancia de la API con base de datos InMemory y dobles
// para el cliente HTTP hacia juegos-servicio. Esto permite cubrir el
// flujo de extremo a extremo (controlador → MediatR → repositorio → DB)
// sin depender de PostgreSQL ni de un juegos-servicio real.
public sealed class FabricaApiPruebas : WebApplicationFactory<Program>
{
    public Mock<IClienteContenidoJuegos> MockClienteContenido { get; } = new();
    public Mock<IClienteIdentidadUsuarios> MockClienteIdentidad { get; } = new();

    // Ids de prueba: el "Administrador" en los tests usa el sub
    // 11111111-1111-1111-1111-111111111111; los operadores usan otros
    // ids. El mock de identidad clasifica como Administrador
    // exactamente a IdAdministradorPrueba.
    public static readonly Guid IdAdministradorPrueba = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid IdTriviaActiva = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid IdBusquedaActiva = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    public static readonly Guid IdTriviaInactiva = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    public static readonly Guid IdContenidoInexistente = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private readonly string _nombreBaseDatos = "UmbralSesionesPruebas-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        constructor.UseEnvironment("Testing");

        // Cadena de conexión y URL de juegos sólo para que la
        // configuración no aborte al arrancar. La cadena no se usa
        // porque el DbContext se reemplaza por InMemory más abajo.
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
                    "Cors:OrigenesPermitidos:0", "http://localhost:3000")
            });
        });

        constructor.ConfigureServices(servicios =>
        {
            // Quita todos los registros de EF + provider Npgsql para que
            // el provider InMemory pueda registrarse limpiamente. Sin
            // este aseo, EF detecta dos providers en el mismo proveedor
            // de servicios y lanza InvalidOperationException.
            var aRemover = servicios.Where(d =>
                d.ServiceType == typeof(DbContextOptions<ContextoSesiones>) ||
                d.ServiceType == typeof(DbContextOptions) ||
                d.ServiceType == typeof(ContextoSesiones) ||
                (d.ServiceType.FullName?.Contains("EntityFrameworkCore", StringComparison.Ordinal) ?? false) ||
                (d.ImplementationType?.FullName?.Contains("Npgsql", StringComparison.Ordinal) ?? false))
                .ToList();
            foreach (var d in aRemover) servicios.Remove(d);

            // Provider EF dedicado a InMemory; al pasar este provider con
            // UseInternalServiceProvider evitamos colisionar con cualquier
            // residuo de Npgsql que pudiera quedar.
            var providerInMemory = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            servicios.AddDbContext<ContextoSesiones>(opciones =>
            {
                opciones.UseInMemoryDatabase(_nombreBaseDatos);
                opciones.UseInternalServiceProvider(providerInMemory);
            });

            // Reemplaza el cliente HTTP del puerto con un Mock que
            // responde con datos predecibles para cada id sembrado.
            var descCliente = servicios.SingleOrDefault(
                d => d.ServiceType == typeof(IClienteContenidoJuegos));
            if (descCliente is not null) servicios.Remove(descCliente);

            ConfigurarMockClienteContenido();
            servicios.AddSingleton(MockClienteContenido.Object);

            // HU34 — Reemplazamos el adaptador HTTP a identidad por un mock.
            // Clasifica como Administrador SÓLO al sub
            // 11111111-1111-1111-1111-111111111111 (IdAdministradorPrueba);
            // el resto se considera Operador o desconocido.
            var descIdentidad = servicios.SingleOrDefault(
                d => d.ServiceType == typeof(IClienteIdentidadUsuarios));
            if (descIdentidad is not null) servicios.Remove(descIdentidad);

            ConfigurarMockClienteIdentidad();
            servicios.AddSingleton(MockClienteIdentidad.Object);

            // Quitamos el HostedService de preparación: en pruebas no
            // queremos que un timer en background mute estados de
            // forma asincrónica (provocaría flakiness).
            var hosted = servicios
                .Where(d => d.ImplementationType?.Name == "ServicioPreparacionSesionesProgramadas")
                .ToList();
            foreach (var d in hosted) servicios.Remove(d);

            // Sustituye JwtBearer por el esquema de pruebas.
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

    private void ConfigurarMockClienteContenido()
    {
        MockClienteContenido
            .Setup(c => c.ObtenerContenidoAsync(
                TipoJuego.Trivia, IdTriviaActiva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContenidoJuegoActivoDto
            {
                Id = IdTriviaActiva, Nombre = "Trivia historia",
                TipoJuego = "Trivia", Estado = "Activa", EstaActivo = true
            });

        MockClienteContenido
            .Setup(c => c.ObtenerContenidoAsync(
                TipoJuego.BusquedaTesoro, IdBusquedaActiva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContenidoJuegoActivoDto
            {
                Id = IdBusquedaActiva, Nombre = "Búsqueda piloto",
                TipoJuego = "BusquedaTesoro", Estado = "Activa", EstaActivo = true
            });

        MockClienteContenido
            .Setup(c => c.ObtenerContenidoAsync(
                TipoJuego.Trivia, IdTriviaInactiva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContenidoJuegoActivoDto
            {
                Id = IdTriviaInactiva, Nombre = "Trivia archivada",
                TipoJuego = "Trivia", Estado = "Archivada", EstaActivo = false
            });

        MockClienteContenido
            .Setup(c => c.ObtenerContenidoAsync(
                It.IsAny<TipoJuego>(), IdContenidoInexistente, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContenidoJuegoActivoDto?)null);

        // HU34/5.2 — Detalle del contenido para enriquecer el detalle
        // de la sesión. Devolvemos un objeto liviano con datos
        // consistentes para no fallar las pruebas que llegan a GET /id.
        MockClienteContenido
            .Setup(c => c.ObtenerDetalleTriviaAsync(IdTriviaActiva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DetalleTriviaSesionDto
            {
                Id = IdTriviaActiva,
                Nombre = "Trivia historia",
                Descripcion = "Demo",
                Estado = "Activa",
                Preguntas = new List<PreguntaTriviaSesionDto>
                {
                    new() {
                        Id = Guid.NewGuid(),
                        Enunciado = "¿Qué año?",
                        PuntajeAsignado = 10,
                        Opciones = new List<OpcionTriviaSesionDto>
                        {
                            new() { Id = Guid.NewGuid(), Texto = "1810", EsCorrecta = true },
                            new() { Id = Guid.NewGuid(), Texto = "1821", EsCorrecta = false }
                        }
                    }
                }
            });

        MockClienteContenido
            .Setup(c => c.ObtenerDetalleBusquedaTesoroAsync(IdBusquedaActiva, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DetalleBusquedaSesionDto
            {
                Id = IdBusquedaActiva,
                Nombre = "Búsqueda piloto",
                Descripcion = "Demo",
                Estado = "Activa",
                Pistas = new List<PistaBusquedaSesionDto>
                {
                    new() { Id = Guid.NewGuid(), Contenido = "Mira al norte" }
                }
            });
    }

    private void ConfigurarMockClienteIdentidad()
    {
        MockClienteIdentidad
            .Setup(c => c.EsAdministradorAsync(
                It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == IdAdministradorPrueba);

        MockClienteIdentidad
            .Setup(c => c.FiltrarAdministradoresAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) =>
                ids.Where(id => id == IdAdministradorPrueba).ToList());
    }
}
