using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteSesionIndividual;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU44 — Orquestación de ExpulsarParticipanteSesionIndividualManejador:
// rol Operador, operador dueño, persistencia y notificación tras guardar.
public class ExpulsarParticipanteSesionIndividualManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Participante = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Sesion? Actualizada;
        public Guid SesionId;
        public Guid ParticipanteSesionId;

        public Contexto(
            SesionIndividual sesion, Guid participanteSesionId,
            Guid? usuarioId = null, string rol = "Operador")
        {
            SesionId = sesion.Id;
            ParticipanteSesionId = participanteSesionId;

            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(usuarioId ?? Operador);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, rol) >= 0);

            Repo.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Callback<Sesion, CancellationToken>((x, _) => Actualizada = x)
                .Returns(Task.CompletedTask);
            Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarParticipantesSesionActualizadosAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarParticipanteExpulsadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarSesionActualizadaAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public ExpulsarParticipanteSesionIndividualManejador Construir()
            => new(Repo.Object, Unidad.Object, Usuario.Object, Notificador.Object,
                Mock.Of<IRegistroLogsAplicacion>());

        public Task Ejecutar()
            => Construir().Handle(
                new ExpulsarParticipanteSesionIndividualComando(SesionId, ParticipanteSesionId),
                CancellationToken.None);
    }

    private static SesionIndividual SesionConParticipante(
        out Guid participanteSesionId, EstadoSesion estado = EstadoSesion.EnPreparacion)
    {
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 10);
        sesion.Preparar();
        participanteSesionId = sesion.AgregarParticipante(Participante, AhoraUtc).Id;
        if (estado == EstadoSesion.EnPreparacion) return sesion;
        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Activa) return sesion;
        if (estado == EstadoSesion.Pausada) { sesion.Pausar(); return sesion; }
        if (estado == EstadoSesion.Finalizada) { sesion.Finalizar(AhoraUtc); return sesion; }
        return sesion;
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Pausada)]
    public async Task OperadorDueno_Expulsa_GuardaYNotifica(EstadoSesion estado)
    {
        var sesion = SesionConParticipante(out var participanteSesionId, estado);
        var ctx = new Contexto(sesion, participanteSesionId);

        await ctx.Ejecutar();

        ((SesionIndividual)ctx.Actualizada!).Participantes.Should().BeEmpty();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            ctx.SesionId, It.IsAny<CancellationToken>()), Times.Once);
        // Aviso dirigido al participante expulsado con su identidad real.
        ctx.Notificador.Verify(n => n.NotificarParticipanteExpulsadoAsync(
            Participante, ctx.SesionId, ctx.ParticipanteSesionId,
            It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            ctx.SesionId, estado.ToString(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OperadorNoDueno_Responde403()
    {
        var sesion = SesionConParticipante(out var participanteSesionId);
        var ctx = new Contexto(sesion, participanteSesionId, usuarioId: Guid.NewGuid());

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Participante")]
    [InlineData("Administrador")]
    public async Task RolNoOperador_Responde403(string rol)
    {
        var sesion = SesionConParticipante(out var participanteSesionId);
        var ctx = new Contexto(sesion, participanteSesionId, rol: rol);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    public async Task EstadoNoPermitido_Responde409(EstadoSesion estado)
    {
        var sesion = SesionConParticipante(out var participanteSesionId, estado);
        var ctx = new Contexto(sesion, participanteSesionId);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ExpulsionNoPermitidaExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SesionInexistente_Responde404()
    {
        var sesion = SesionConParticipante(out var participanteSesionId);
        var ctx = new Contexto(sesion, participanteSesionId);
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task ParticipanteInexistente_Responde404()
    {
        var sesion = SesionConParticipante(out _);
        var ctx = new Contexto(sesion, Guid.NewGuid());

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ParticipanteNoEncontradoExcepcion>();
    }

    [Fact]
    public async Task SesionGrupal_Rechaza()
    {
        var grupal = SesionGrupal.Crear(
            "Grupal", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 5, 3);
        grupal.Preparar();
        var ctx = new Contexto(SesionConParticipante(out var pid), pid);
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grupal);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<SesionInvalidaExcepcion>();
    }

    [Fact]
    public async Task SiGuardarFalla_NoNotifica()
    {
        var sesion = SesionConParticipante(out var participanteSesionId);
        var ctx = new Contexto(sesion, participanteSesionId);
        ctx.Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo db"));

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<InvalidOperationException>();
        ctx.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarParticipanteExpulsadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
