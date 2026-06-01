using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.CasosDeUso.Consultas;
using SesionesServicio.Aplicacion.CasosDeUso.Manejadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU34/5.2 — Visibilidad por rol y enriquecimiento del contenido en
// el detalle de sesión. La regla "creada por Administrador" la
// resuelve identidad-servicio en línea (IClienteIdentidadUsuarios).
public class ObtenerSesionPorIdManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 5, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Admin = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid OperadorA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OperadorB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ContenidoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static Sesion CrearSesion(
        Guid creador,
        TipoJuego tipoJuego = TipoJuego.Trivia,
        Guid? id = null,
        Guid? contenido = null)
        => Sesion.Rehidratar(
            id ?? Guid.NewGuid(),
            "Sesión",
            tipoJuego,
            contenido ?? ContenidoId,
            ModoSesion.Individual,
            EstadoSesion.Programada,
            AhoraUtc.AddHours(1),
            creador,
            AhoraUtc);

    private sealed class Fabrica
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IClienteContenidoJuegos> Cliente { get; } = new();
        public Mock<IClienteIdentidadUsuarios> Identidad { get; } = new();

        public Fabrica(string[] roles, Guid? usuarioId, bool autenticado = true)
        {
            Usuario.Setup(u => u.EstaAutenticado).Returns(autenticado);
            Usuario.Setup(u => u.Id).Returns(usuarioId);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(c => c.Any(roles.Contains));
        }

        public ObtenerSesionPorIdManejador Construir() =>
            new(Repo.Object, Usuario.Object, Cliente.Object, Identidad.Object);
    }

    private static DetalleTriviaSesionDto DetalleTriviaDummy() => new()
    {
        Id = ContenidoId,
        Nombre = "Trivia historia",
        Descripcion = "Demo",
        Estado = "Activa"
    };

    private static DetalleBusquedaSesionDto DetalleBusquedaDummy() => new()
    {
        Id = ContenidoId,
        Nombre = "Búsqueda piloto",
        Descripcion = "Demo",
        Estado = "Activa"
    };

    [Fact]
    public async Task NoAutenticado_DebeLanzarUsuarioNoAutorizado()
    {
        var f = new Fabrica(Array.Empty<string>(), null, autenticado: false);
        Func<Task> a = () => f.Construir().Handle(
            new ObtenerSesionPorIdConsulta(Guid.NewGuid()), CancellationToken.None);
        await a.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
    }

    [Fact]
    public async Task Participante_DebeLanzarUsuarioNoAutorizado()
    {
        var f = new Fabrica(new[] { "Participante" }, Guid.NewGuid());
        Func<Task> a = () => f.Construir().Handle(
            new ObtenerSesionPorIdConsulta(Guid.NewGuid()), CancellationToken.None);
        await a.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
    }

    [Fact]
    public async Task SesionInexistente_DebeDevolverNull()
    {
        var f = new Fabrica(new[] { "Administrador" }, Admin);
        f.Repo.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        var resultado = await f.Construir().Handle(
            new ObtenerSesionPorIdConsulta(Guid.NewGuid()), CancellationToken.None);

        resultado.Should().BeNull();
    }

    [Fact]
    public async Task Administrador_VeDetalleDeCualquierSesion_Trivia()
    {
        var sesion = CrearSesion(OperadorA, TipoJuego.Trivia);
        var f = new Fabrica(new[] { "Administrador" }, Admin);
        f.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        f.Cliente.Setup(c => c.ObtenerDetalleTriviaAsync(ContenidoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetalleTriviaDummy());

        var detalle = await f.Construir().Handle(
            new ObtenerSesionPorIdConsulta(sesion.Id), CancellationToken.None);

        detalle.Should().NotBeNull();
        detalle!.Trivia.Should().NotBeNull();
        detalle.BusquedaTesoro.Should().BeNull();

        // Administrador no debe disparar la consulta a identidad.
        f.Identidad.Verify(i => i.EsAdministradorAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Administrador_VeDetalleDeBusqueda()
    {
        var sesion = CrearSesion(OperadorA, TipoJuego.BusquedaTesoro);
        var f = new Fabrica(new[] { "Administrador" }, Admin);
        f.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        f.Cliente.Setup(c => c.ObtenerDetalleBusquedaTesoroAsync(ContenidoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetalleBusquedaDummy());

        var detalle = await f.Construir().Handle(
            new ObtenerSesionPorIdConsulta(sesion.Id), CancellationToken.None);

        detalle!.BusquedaTesoro.Should().NotBeNull();
        detalle.Trivia.Should().BeNull();
    }

    [Fact]
    public async Task Operador_VeSuPropiaSesion_SinConsultarIdentidad()
    {
        var sesion = CrearSesion(OperadorA);
        var f = new Fabrica(new[] { "Operador" }, OperadorA);
        f.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        f.Cliente.Setup(c => c.ObtenerDetalleTriviaAsync(ContenidoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetalleTriviaDummy());

        var detalle = await f.Construir().Handle(
            new ObtenerSesionPorIdConsulta(sesion.Id), CancellationToken.None);

        detalle.Should().NotBeNull();
        f.Identidad.Verify(i => i.EsAdministradorAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Operador_VeSesionCuandoIdentidadDiceQueElCreadorEsAdministrador()
    {
        var sesion = CrearSesion(Admin);
        var f = new Fabrica(new[] { "Operador" }, OperadorA);
        f.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        f.Identidad.Setup(i => i.EsAdministradorAsync(Admin, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        f.Cliente.Setup(c => c.ObtenerDetalleTriviaAsync(ContenidoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(DetalleTriviaDummy());

        var detalle = await f.Construir().Handle(
            new ObtenerSesionPorIdConsulta(sesion.Id), CancellationToken.None);

        detalle.Should().NotBeNull();
        f.Identidad.Verify(i => i.EsAdministradorAsync(Admin, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Operador_NoVeSesionCuandoIdentidadDiceQueElCreadorNoEsAdministrador()
    {
        var sesion = CrearSesion(OperadorB);
        var f = new Fabrica(new[] { "Operador" }, OperadorA);
        f.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        f.Identidad.Setup(i => i.EsAdministradorAsync(OperadorB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Func<Task> a = () => f.Construir().Handle(
            new ObtenerSesionPorIdConsulta(sesion.Id), CancellationToken.None);

        await a.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
        f.Cliente.Verify(c => c.ObtenerDetalleTriviaAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ContenidoTriviaNoDisponible_DebeLanzarContenidoSesionNoDisponible()
    {
        var sesion = CrearSesion(Admin, TipoJuego.Trivia);
        var f = new Fabrica(new[] { "Administrador" }, Admin);
        f.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        f.Cliente.Setup(c => c.ObtenerDetalleTriviaAsync(ContenidoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DetalleTriviaSesionDto?)null);

        Func<Task> a = () => f.Construir().Handle(
            new ObtenerSesionPorIdConsulta(sesion.Id), CancellationToken.None);

        await a.Should().ThrowAsync<ContenidoSesionNoDisponibleExcepcion>();
    }

    [Fact]
    public void Dto_NoExponeCreadaPorRol()
    {
        typeof(SesionDetalleDto).GetProperty("CreadaPorRol").Should().BeNull();
    }
}
