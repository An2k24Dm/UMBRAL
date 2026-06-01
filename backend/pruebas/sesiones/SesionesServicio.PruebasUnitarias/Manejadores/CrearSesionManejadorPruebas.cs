using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.CasosDeUso.Manejadores;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU33 — Pruebas unitarias del manejador CrearSesion. Cubren autorización,
// validación de contenido y persistencia con dobles para los puertos.
//
// HU34 — La sesión NO guarda el rol del creador, así que no hay ningún
// aserto sobre CreadaPorRol: el rol se resuelve al consultar y no al
// crear.
public class CrearSesionManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 5, 29, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ContenidoJuegoId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid CreadorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static CrearSesionSolicitudDto SolicitudValida(string tipoJuego = "Trivia") => new()
    {
        Nombre = "Sesión piloto",
        TipoJuego = tipoJuego,
        ContenidoJuegoId = ContenidoJuegoId,
        Modo = "Individual",
        FechaProgramada = AhoraUtc.AddHours(2)
    };

    private static ContenidoJuegoActivoDto ContenidoActivo(string tipoJuego, bool activo = true) => new()
    {
        Id = ContenidoJuegoId,
        Nombre = tipoJuego == "Trivia" ? "Trivia historia" : "Búsqueda piloto",
        TipoJuego = tipoJuego,
        Estado = activo ? "Activa" : "Archivada",
        EstaActivo = activo
    };

    private sealed class Fabrica
    {
        public Mock<IRepositorioSesiones> Repositorio { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<IClienteContenidoJuegos> Cliente { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();

        public Fabrica(string[]? roles = null, bool autenticado = true)
        {
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            Usuario.Setup(u => u.EstaAutenticado).Returns(autenticado);
            Usuario.Setup(u => u.Id).Returns(autenticado ? CreadorId : (Guid?)null);
            Usuario.Setup(u => u.NombreUsuario).Returns(autenticado ? "admin" : null);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(consulta =>
                {
                    var actuales = roles ?? Array.Empty<string>();
                    foreach (var rol in consulta)
                        if (Array.Exists(actuales, x => x == rol)) return true;
                    return false;
                });
            Usuario.Setup(u => u.Roles).Returns(roles ?? Array.Empty<string>());
        }

        public CrearSesionManejador Construir() =>
            new(new ValidadorCrearSesion(),
                Repositorio.Object,
                Unidad.Object,
                Usuario.Object,
                Cliente.Object,
                Reloj.Object);
    }

    [Theory]
    [InlineData("Administrador", "Trivia")]
    [InlineData("Operador", "Trivia")]
    [InlineData("Administrador", "BusquedaTesoro")]
    [InlineData("Operador", "BusquedaTesoro")]
    public async Task RolPermitido_ConContenidoActivo_DebeCrearSesionProgramada(string rol, string tipoJuego)
    {
        var fabrica = new Fabrica(new[] { rol });
        var esperado = Enum.Parse<TipoJuego>(tipoJuego);

        fabrica.Cliente
            .Setup(c => c.ObtenerContenidoAsync(esperado, ContenidoJuegoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContenidoActivo(tipoJuego));

        Sesion? sesionPersistida = null;
        fabrica.Repositorio
            .Setup(r => r.AgregarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
            .Callback<Sesion, CancellationToken>((s, _) => sesionPersistida = s)
            .Returns(Task.CompletedTask);

        var manejador = fabrica.Construir();
        var respuesta = await manejador.Handle(
            new CrearSesionComando(SolicitudValida(tipoJuego)), CancellationToken.None);

        respuesta.Estado.Should().Be("Programada");
        respuesta.TipoJuego.Should().Be(tipoJuego);
        respuesta.Modo.Should().Be("Individual");
        respuesta.ContenidoJuegoId.Should().Be(ContenidoJuegoId);
        respuesta.CreadaPorUsuarioId.Should().Be(CreadorId);
        respuesta.FechaCreacion.Should().Be(AhoraUtc);

        sesionPersistida.Should().NotBeNull();
        sesionPersistida!.Estado.Should().Be(EstadoSesion.Programada);
        sesionPersistida.TipoJuego.Should().Be(esperado);
        sesionPersistida.ContenidoJuegoId.Should().Be(ContenidoJuegoId);
        sesionPersistida.Modo.Should().Be(ModoSesion.Individual);
        sesionPersistida.FechaProgramada.Should().Be(SolicitudValida(tipoJuego).FechaProgramada);
        sesionPersistida.CreadaPorUsuarioId.Should().Be(CreadorId);

        fabrica.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UsuarioAnonimo_NoDebeCrearSesion()
    {
        var fabrica = new Fabrica(roles: Array.Empty<string>(), autenticado: false);
        var manejador = fabrica.Construir();

        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(SolicitudValida()), CancellationToken.None);

        await accion.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
        fabrica.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Participante_NoDebeCrearSesion()
    {
        var fabrica = new Fabrica(new[] { "Participante" });
        var manejador = fabrica.Construir();

        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(SolicitudValida()), CancellationToken.None);

        await accion.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
        fabrica.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ContenidoInexistente_DebeLanzarContenidoNoEncontrado()
    {
        var fabrica = new Fabrica(new[] { "Administrador" });
        fabrica.Cliente
            .Setup(c => c.ObtenerContenidoAsync(It.IsAny<TipoJuego>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContenidoJuegoActivoDto?)null);

        var manejador = fabrica.Construir();

        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(SolicitudValida()), CancellationToken.None);

        await accion.Should().ThrowAsync<ContenidoJuegoNoEncontradoExcepcion>();
    }

    [Fact]
    public async Task ContenidoInactivo_DebeLanzarContenidoNoActivo()
    {
        var fabrica = new Fabrica(new[] { "Operador" });
        fabrica.Cliente
            .Setup(c => c.ObtenerContenidoAsync(TipoJuego.Trivia, ContenidoJuegoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContenidoActivo("Trivia", activo: false));

        var manejador = fabrica.Construir();

        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(SolicitudValida()), CancellationToken.None);

        await accion.Should().ThrowAsync<ContenidoJuegoNoActivoExcepcion>();
    }

    [Fact]
    public async Task TipoJuegoInvalido_DebeLanzarValidacion()
    {
        var fabrica = new Fabrica(new[] { "Administrador" });
        var solicitud = SolicitudValida();
        solicitud.TipoJuego = "Otro";

        var manejador = fabrica.Construir();
        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(solicitud), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task ModoInvalido_DebeLanzarValidacion()
    {
        var fabrica = new Fabrica(new[] { "Administrador" });
        var solicitud = SolicitudValida();
        solicitud.Modo = "Coop";

        var manejador = fabrica.Construir();
        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(solicitud), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task ContenidoJuegoIdVacio_DebeLanzarValidacion()
    {
        var fabrica = new Fabrica(new[] { "Administrador" });
        var solicitud = SolicitudValida();
        solicitud.ContenidoJuegoId = Guid.Empty;

        var manejador = fabrica.Construir();
        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(solicitud), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task NombreVacio_DebeLanzarValidacion()
    {
        var fabrica = new Fabrica(new[] { "Administrador" });
        var solicitud = SolicitudValida();
        solicitud.Nombre = "  ";

        var manejador = fabrica.Construir();
        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(solicitud), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
    }

    [Fact]
    public async Task FechaProgramada_Pasada_DebeLanzarSesionInvalida_YNoPersistir()
    {
        var fabrica = new Fabrica(new[] { "Administrador" });
        var solicitud = SolicitudValida();
        solicitud.FechaProgramada = AhoraUtc.AddMinutes(-1);

        var manejador = fabrica.Construir();
        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(solicitud), CancellationToken.None);

        await accion.Should()
            .ThrowAsync<SesionInvalidaExcepcion>()
            .WithMessage("La sesión no puede programarse para una fecha y hora que ya pasó.");

        fabrica.Repositorio.Verify(
            r => r.AgregarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()),
            Times.Never);
        fabrica.Unidad.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
        fabrica.Cliente.Verify(
            c => c.ObtenerContenidoAsync(
                It.IsAny<TipoJuego>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FechaProgramada_IgualAhora_DebeLanzarSesionInvalida()
    {
        var fabrica = new Fabrica(new[] { "Operador" });
        var solicitud = SolicitudValida();
        solicitud.FechaProgramada = AhoraUtc;

        var manejador = fabrica.Construir();
        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(solicitud), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionInvalidaExcepcion>();
    }

    [Fact]
    public async Task FechaProgramada_Futura_DebeAceptarse()
    {
        var fabrica = new Fabrica(new[] { "Administrador" });
        fabrica.Cliente
            .Setup(c => c.ObtenerContenidoAsync(TipoJuego.Trivia, ContenidoJuegoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContenidoActivo("Trivia"));

        var solicitud = SolicitudValida();
        solicitud.FechaProgramada = AhoraUtc.AddSeconds(1);

        var manejador = fabrica.Construir();
        var respuesta = await manejador.Handle(
            new CrearSesionComando(solicitud), CancellationToken.None);

        respuesta.Estado.Should().Be("Programada");
        fabrica.Unidad.Verify(
            u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PoliticaFecha_UsaProveedorFechaHora_NoUtcNow()
    {
        // El reloj inyectado retorna AhoraUtc (29-may-2026). Si pasamos
        // una fecha "pasada" relativa a AhoraUtc pero "futura" relativa
        // al reloj real del sistema, el manejador igualmente debe
        // rechazarla. Eso prueba que la comparación va contra el reloj
        // inyectado, no contra DateTime.UtcNow.
        var fabrica = new Fabrica(new[] { "Administrador" });
        var solicitud = SolicitudValida();
        solicitud.FechaProgramada = AhoraUtc.AddMinutes(-1);

        var manejador = fabrica.Construir();
        Func<Task> accion = () => manejador.Handle(
            new CrearSesionComando(solicitud), CancellationToken.None);

        await accion.Should().ThrowAsync<SesionInvalidaExcepcion>();
        fabrica.Reloj.Verify(r => r.ObtenerFechaHoraUtc(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RespuestaDto_NoDebeExponerCreadaPorRol()
    {
        var fabrica = new Fabrica(new[] { "Administrador" });
        fabrica.Cliente
            .Setup(c => c.ObtenerContenidoAsync(TipoJuego.Trivia, ContenidoJuegoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ContenidoActivo("Trivia"));

        var manejador = fabrica.Construir();
        var respuesta = await manejador.Handle(
            new CrearSesionComando(SolicitudValida()), CancellationToken.None);

        // Defensa estructural: el DTO no debe traer la propiedad CreadaPorRol.
        typeof(CrearSesionRespuestaDto).GetProperty("CreadaPorRol").Should().BeNull();
        respuesta.Should().NotBeNull();
    }
}
