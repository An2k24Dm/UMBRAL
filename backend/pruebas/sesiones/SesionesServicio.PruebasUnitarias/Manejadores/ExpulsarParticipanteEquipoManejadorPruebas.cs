using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteEquipo;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU45 — Orquestación de ExpulsarParticipanteEquipoManejador: roles
// (líder u Operador dueño; nunca Administrador), persistencia y las tres
// notificaciones (equipos, equipo y aviso dirigido) tras guardar.
public class ExpulsarParticipanteEquipoManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid LiderIdentidad = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid MiembroIdentidad = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Sesion? Actualizada;
        public Guid SesionId;
        public Guid EquipoId;
        public Guid ObjetivoId;

        public Contexto(
            Sesion sesion, Guid equipoId, Guid objetivoId,
            Guid? usuarioId = null, string rol = "Operador")
        {
            SesionId = sesion.Id;
            EquipoId = equipoId;
            ObjetivoId = objetivoId;

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
            Notificador.Setup(n => n.NotificarEquiposSesionActualizadosAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquipoActualizadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarParticipanteExpulsadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public ExpulsarParticipanteEquipoManejador Construir()
            => new(
                new ExpulsarParticipanteEquipoValidador(),
                Repo.Object,
                Unidad.Object,
                Usuario.Object,
                Notificador.Object);

        public Task Ejecutar()
            => Construir().Handle(
                new ExpulsarParticipanteEquipoComando(SesionId, EquipoId, ObjetivoId),
                CancellationToken.None);
    }

    private static SesionGrupal SesionConEquipo(
        out Guid equipoId, out Guid liderSesionId, out Guid miembroSesionId,
        EstadoSesion estado = EstadoSesion.EnPreparacion)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 5, 3);
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null,
            LiderIdentidad, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;
        liderSesionId = equipo.LiderParticipanteId;
        miembroSesionId = sesion.AgregarParticipanteAEquipo(
            equipo.Id, MiembroIdentidad, AhoraUtc.AddMinutes(1), AhoraUtc.AddMinutes(1)).Id;

        if (estado == EstadoSesion.Programada) return sesion;
        sesion.Preparar();
        if (estado == EstadoSesion.EnPreparacion) return sesion;
        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Activa) return sesion;
        if (estado == EstadoSesion.Pausada) { sesion.Pausar(); return sesion; }
        if (estado == EstadoSesion.Finalizada) { sesion.Finalizar(AhoraUtc); return sesion; }
        if (estado == EstadoSesion.Cancelada) { sesion.Cancelar(); return sesion; }
        return sesion;
    }

    [Theory]
    [InlineData(EstadoSesion.EnPreparacion)]
    [InlineData(EstadoSesion.Pausada)]
    public async Task OperadorDueno_Expulsa_GuardaYNotifica(EstadoSesion estado)
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId, estado);
        var ctx = new Contexto(sesion, equipoId, miembroId);

        await ctx.Ejecutar();

        ((SesionGrupal)ctx.Actualizada!).Equipos.Single().Participantes
            .Should().NotContain(p => p.Id == miembroId);
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            ctx.SesionId, equipoId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            ctx.SesionId, equipoId, It.IsAny<CancellationToken>()), Times.Once);
        // Aviso dirigido al expulsado con su identidad real.
        ctx.Notificador.Verify(n => n.NotificarParticipanteExpulsadoAsync(
            MiembroIdentidad, ctx.SesionId, miembroId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OperadorNoDueno_Responde403()
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId);
        var ctx = new Contexto(sesion, equipoId, miembroId, usuarioId: Guid.NewGuid());

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Administrador_NoPuedeExpulsar()
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId);
        var ctx = new Contexto(sesion, equipoId, miembroId, rol: "Administrador");

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task ParticipanteLider_ExpulsaIntegranteNormal()
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId);
        var ctx = new Contexto(
            sesion, equipoId, miembroId,
            usuarioId: LiderIdentidad, rol: "Participante");

        await ctx.Ejecutar();

        ((SesionGrupal)ctx.Actualizada!).Equipos.Single().Participantes
            .Should().ContainSingle(p => p.ParticipanteIdentidadId == LiderIdentidad);
        ctx.Notificador.Verify(n => n.NotificarParticipanteExpulsadoAsync(
            MiembroIdentidad, ctx.SesionId, miembroId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ParticipanteNoLider_NoPuedeExpulsar()
    {
        var sesion = SesionConEquipo(out var equipoId, out var liderSesionId, out _);
        var ctx = new Contexto(
            sesion, equipoId, liderSesionId,
            usuarioId: MiembroIdentidad, rol: "Participante");

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>()
            .WithMessage("Solo el líder del equipo puede expulsar participantes.");
    }

    [Fact]
    public async Task ParticipanteLider_NoPuedeExpulsarseNiExpulsarAlLider()
    {
        var sesion = SesionConEquipo(out var equipoId, out var liderSesionId, out _);
        var ctx = new Contexto(
            sesion, equipoId, liderSesionId,
            usuarioId: LiderIdentidad, rol: "Participante");

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<EquipoInvalidoExcepcion>()
            .WithMessage("No puedes expulsar al líder del equipo.");
    }

    [Fact]
    public async Task OperadorExpulsaLider_ReasignaLiderazgo()
    {
        var sesion = SesionConEquipo(out var equipoId, out var liderSesionId, out var miembroId);
        var ctx = new Contexto(sesion, equipoId, liderSesionId);

        await ctx.Ejecutar();

        var equipo = ((SesionGrupal)ctx.Actualizada!).Equipos.Single();
        equipo.LiderParticipanteId.Should().Be(miembroId);
        ctx.Notificador.Verify(n => n.NotificarParticipanteExpulsadoAsync(
            LiderIdentidad, ctx.SesionId, liderSesionId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SesionInexistente_Responde404()
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId);
        var ctx = new Contexto(sesion, equipoId, miembroId);
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task SesionIndividual_Rechaza()
    {
        var individual = SesionIndividual.Crear(
            "Ind", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 10);
        individual.Preparar();
        var ctx = new Contexto(
            SesionConEquipo(out var equipoId, out _, out var miembroId),
            equipoId, miembroId);
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(individual);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<SesionNoGrupalExcepcion>();
    }

    [Fact]
    public async Task EquipoInexistente_Responde404()
    {
        var sesion = SesionConEquipo(out _, out _, out var miembroId);
        var ctx = new Contexto(sesion, Guid.NewGuid(), miembroId);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<EquipoNoEncontradoExcepcion>();
    }

    [Fact]
    public async Task ParticipanteInexistente_Responde404()
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out _);
        var ctx = new Contexto(sesion, equipoId, Guid.NewGuid());

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ParticipanteNoEncontradoExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.Programada)]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task EstadoNoPermitido_Responde409(EstadoSesion estado)
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId, estado);
        var ctx = new Contexto(sesion, equipoId, miembroId);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ExpulsionNoPermitidaExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SiGuardarFalla_NoNotifica()
    {
        var sesion = SesionConEquipo(out var equipoId, out _, out var miembroId);
        var ctx = new Contexto(sesion, equipoId, miembroId);
        ctx.Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo db"));

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<InvalidOperationException>();
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarParticipanteExpulsadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
