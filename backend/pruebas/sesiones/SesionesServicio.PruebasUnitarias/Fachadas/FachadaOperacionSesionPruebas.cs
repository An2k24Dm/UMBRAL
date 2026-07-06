using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.CancelarSesion;
using SesionesServicio.Aplicacion.Comandos.IniciarSesion;
using SesionesServicio.Aplicacion.Comandos.PausarSesion;
using SesionesServicio.Aplicacion.Comandos.ReanudarSesion;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Fachadas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones.OperacionSesion;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Fachadas;

// Patrón Facade (HU52): coordinación de las operaciones del ciclo de vida de
// una sesión (iniciar, pausar, reanudar, cancelar) sobre el patrón State del
// dominio. Se verifica autorización, reglas de estado/negocio, persistencia y
// notificación en tiempo real.
public class FachadaOperacionSesionPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 3, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid OtroOperador = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid SesionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ParticipanteId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();

        public Contexto(Sesion? sesion, string rol = "Operador", Guid? usuarioId = null)
        {
            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(usuarioId ?? Operador);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, rol) >= 0);
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            Repo.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarSesionActualizadaAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public FachadaOperacionSesion Crear()
            => new(Repo.Object, Unidad.Object, Usuario.Object, Notificador.Object,
                Reloj.Object, Mock.Of<IRegistroLogsAplicacion>(),
                new ValidadorInicioSesionOperacion(),
                new ValidadorCancelacionSesionOperacion());
    }

    // Sesión rehidratada en el estado indicado, con (o sin) un inscrito.
    private static SesionIndividual Sesion(
        EstadoSesion estado,
        DateTime? fechaProgramada = null,
        Guid? operador = null,
        bool conInscrito = true)
    {
        var participantes = conInscrito
            ? new[] { Participante.CrearParaSesionIndividual(SesionId, ParticipanteId, AhoraUtc) }
            : Array.Empty<Participante>();

        var yaIniciada = estado is EstadoSesion.Activa
            or EstadoSesion.Pausada or EstadoSesion.Finalizada;

        return SesionIndividual.Rehidratar(
            SesionId, "Sesión", "Demo", estado,
            fechaProgramada ?? AhoraUtc.AddHours(1), "ABC123",
            operador ?? Operador, AhoraUtc,
            yaIniciada ? AhoraUtc : null,
            estado == EstadoSesion.Finalizada ? AhoraUtc : null,
            10, null, participantes);
    }

    // 1
    [Fact]
    public async Task Iniciar_SesionEnPreparacion_PasaAActiva()
    {
        var ctx = new Contexto(Sesion(EstadoSesion.EnPreparacion));

        var resultado = await ctx.Crear().IniciarAsync(SesionId, CancellationToken.None);

        resultado.Estado.Should().Be("Activa");
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // 2
    [Fact]
    public async Task Iniciar_ProgramadaConFechaVencida_PreparaYActiva()
    {
        var ctx = new Contexto(Sesion(EstadoSesion.Programada, fechaProgramada: AhoraUtc.AddHours(-1)));

        var resultado = await ctx.Crear().IniciarAsync(SesionId, CancellationToken.None);

        resultado.Estado.Should().Be("Activa");
        resultado.FechaInicioUtc.Should().Be(AhoraUtc);
    }

    // 3
    [Fact]
    public async Task Iniciar_ProgramadaConFechaFutura_Falla()
    {
        var ctx = new Contexto(Sesion(EstadoSesion.Programada, fechaProgramada: AhoraUtc.AddHours(1)));

        Func<Task> accion = () => ctx.Crear().IniciarAsync(SesionId, CancellationToken.None);

        await accion.Should().ThrowAsync<OperacionSesionInvalidaExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // 4
    [Fact]
    public async Task Iniciar_SinInscritos_Falla()
    {
        var ctx = new Contexto(Sesion(EstadoSesion.EnPreparacion, conInscrito: false));

        Func<Task> accion = () => ctx.Crear().IniciarAsync(SesionId, CancellationToken.None);

        await accion.Should().ThrowAsync<OperacionSesionInvalidaExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // 4b — iniciar desde un estado inválido (con inscritos): lo rechaza el State.
    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task Iniciar_DesdeEstadoInvalido_LanzaTransicion(EstadoSesion estado)
    {
        var ctx = new Contexto(Sesion(estado));

        Func<Task> accion = () => ctx.Crear().IniciarAsync(SesionId, CancellationToken.None);

        await accion.Should().ThrowAsync<TransicionEstadoSesionInvalidaExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // 5
    [Fact]
    public async Task Pausar_SesionActiva_PasaAPausada()
    {
        var ctx = new Contexto(Sesion(EstadoSesion.Activa));

        var resultado = await ctx.Crear().PausarAsync(SesionId, CancellationToken.None);

        resultado.Estado.Should().Be("Pausada");
    }

    // 6 — pausar desde un estado != Activa: lo rechaza el patrón State.
    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task Pausar_SesionNoActiva_Falla(EstadoSesion estado)
    {
        var ctx = new Contexto(Sesion(estado));

        Func<Task> accion = () => ctx.Crear().PausarAsync(SesionId, CancellationToken.None);

        await accion.Should().ThrowAsync<TransicionEstadoSesionInvalidaExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // 7
    [Fact]
    public async Task Reanudar_SesionPausada_PasaAActiva()
    {
        var ctx = new Contexto(Sesion(EstadoSesion.Pausada));

        var resultado = await ctx.Crear().ReanudarAsync(SesionId, CancellationToken.None);

        resultado.Estado.Should().Be("Activa");
    }

    // 7b — reanudar desde un estado != Pausada: lo rechaza el patrón State.
    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task Reanudar_SesionNoPausada_Falla(EstadoSesion estado)
    {
        var ctx = new Contexto(Sesion(estado));

        Func<Task> accion = () => ctx.Crear().ReanudarAsync(SesionId, CancellationToken.None);

        await accion.Should().ThrowAsync<TransicionEstadoSesionInvalidaExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // 8
    [Fact]
    public async Task Cancelar_SesionActiva_PasaACancelada()
    {
        var ctx = new Contexto(Sesion(EstadoSesion.Activa));

        var resultado = await ctx.Crear().CancelarAsync(SesionId, CancellationToken.None);

        resultado.Estado.Should().Be("Cancelada");
    }

    // 9a — Programada: regla extra de aplicación (se elimina, no se cancela).
    [Fact]
    public async Task Cancelar_Programada_Falla()
    {
        var ctx = new Contexto(Sesion(EstadoSesion.Programada));

        Func<Task> accion = () => ctx.Crear().CancelarAsync(SesionId, CancellationToken.None);

        (await accion.Should().ThrowAsync<OperacionSesionInvalidaExcepcion>())
            .WithMessage("*debe eliminarse*");
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // 9b — Finalizada/Cancelada: transición inválida a cargo del patrón State.
    [Theory]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task Cancelar_FinalizadaOCancelada_Falla(EstadoSesion estado)
    {
        var ctx = new Contexto(Sesion(estado));

        Func<Task> accion = () => ctx.Crear().CancelarAsync(SesionId, CancellationToken.None);

        await accion.Should().ThrowAsync<TransicionEstadoSesionInvalidaExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // 10
    [Fact]
    public async Task OperadorNoDueno_NoPuedeOperar()
    {
        var ctx = new Contexto(
            Sesion(EstadoSesion.Activa, operador: OtroOperador),
            rol: "Operador", usuarioId: Operador);

        Func<Task> accion = () => ctx.Crear().PausarAsync(SesionId, CancellationToken.None);

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // (extra) roles no Operador quedan fuera.
    [Theory]
    [InlineData("Administrador")]
    [InlineData("Participante")]
    public async Task RolNoOperador_NoPuedeOperar(string rol)
    {
        var ctx = new Contexto(Sesion(EstadoSesion.Activa), rol: rol);

        Func<Task> accion = () => ctx.Crear().PausarAsync(SesionId, CancellationToken.None);

        await accion.Should().ThrowAsync<UsuarioNoAutorizadoCrearSesionExcepcion>();
    }

    // (extra) sesión inexistente.
    [Fact]
    public async Task SesionInexistente_LanzaNoEncontrada()
    {
        var ctx = new Contexto(sesion: null);

        Func<Task> accion = () => ctx.Crear().IniciarAsync(SesionId, CancellationToken.None);

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    // 11 — notifica SesionActualizada después de guardar.
    [Fact]
    public async Task Operacion_NotificaDespuesDeGuardar()
    {
        var ctx = new Contexto(Sesion(EstadoSesion.Activa));
        var orden = new List<string>();
        ctx.Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("guardar")).Returns(Task.CompletedTask);
        ctx.Notificador.Setup(n => n.NotificarSesionActualizadaAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => orden.Add("notificar")).Returns(Task.CompletedTask);

        await ctx.Crear().PausarAsync(SesionId, CancellationToken.None);

        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            It.IsAny<Guid>(), "Pausada", It.IsAny<CancellationToken>()), Times.Once);
        orden.Should().Equal("guardar", "notificar");
    }

    // 9 (Part 9) — sin modelo de progreso, la finalización automática no ocurre.
    [Fact]
    public async Task FinalizarSiCorresponde_SinModeloDeProgreso_RetornaNull()
    {
        var ctx = new Contexto(Sesion(EstadoSesion.Activa));

        var resultado = await ctx.Crear().FinalizarSiCorrespondeAsync(SesionId, CancellationToken.None);

        resultado.Should().BeNull();
    }

    // 12 — los manejadores CQRS solo delegan en la fachada.
    [Fact]
    public async Task Manejadores_DeleganEnLaFachada()
    {
        var fachada = new Mock<IFachadaOperacionSesion>();
        var dto = new OperacionSesionRespuestaDto { SesionId = SesionId, Estado = "Activa" };
        fachada.Setup(f => f.IniciarAsync(SesionId, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        fachada.Setup(f => f.PausarAsync(SesionId, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        fachada.Setup(f => f.ReanudarAsync(SesionId, It.IsAny<CancellationToken>())).ReturnsAsync(dto);
        fachada.Setup(f => f.CancelarAsync(SesionId, It.IsAny<CancellationToken>())).ReturnsAsync(dto);

        (await new IniciarSesionManejador(fachada.Object)
            .Handle(new IniciarSesionComando(SesionId), CancellationToken.None)).Should().BeSameAs(dto);
        (await new PausarSesionManejador(fachada.Object)
            .Handle(new PausarSesionComando(SesionId), CancellationToken.None)).Should().BeSameAs(dto);
        (await new ReanudarSesionManejador(fachada.Object)
            .Handle(new ReanudarSesionComando(SesionId), CancellationToken.None)).Should().BeSameAs(dto);
        (await new CancelarSesionManejador(fachada.Object)
            .Handle(new CancelarSesionComando(SesionId), CancellationToken.None)).Should().BeSameAs(dto);

        fachada.Verify(f => f.IniciarAsync(SesionId, It.IsAny<CancellationToken>()), Times.Once);
        fachada.Verify(f => f.PausarAsync(SesionId, It.IsAny<CancellationToken>()), Times.Once);
        fachada.Verify(f => f.ReanudarAsync(SesionId, It.IsAny<CancellationToken>()), Times.Once);
        fachada.Verify(f => f.CancelarAsync(SesionId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
