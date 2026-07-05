using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.ExpulsarEquipoSesionGrupal;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU44 — Orquestación de ExpulsarEquipoSesionGrupalManejador: rol Operador,
// operador dueño, persistencia y notificación tras guardar.
public class ExpulsarEquipoSesionGrupalManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 30, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Lider = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid Integrante = Guid.Parse("66666666-6666-6666-6666-666666666666");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<IUsuarioActual> Usuario { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Sesion? Actualizada;
        public Guid SesionId;
        public Guid EquipoId;

        public Contexto(
            SesionGrupal sesion, Guid equipoId,
            Guid? usuarioId = null, string rol = "Operador")
        {
            SesionId = sesion.Id;
            EquipoId = equipoId;

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
        }

        public ExpulsarEquipoSesionGrupalManejador Construir()
            => new(Repo.Object, Unidad.Object, Usuario.Object, Notificador.Object,
                Mock.Of<IRegistroLogsAplicacion>());

        public Task Ejecutar()
            => Construir().Handle(
                new ExpulsarEquipoSesionGrupalComando(SesionId, EquipoId),
                CancellationToken.None);
    }

    private static SesionGrupal SesionConEquipo(
        out Guid equipoId, EstadoSesion estado = EstadoSesion.EnPreparacion)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 5, 3);
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;
        if (estado == EstadoSesion.Programada) return sesion;
        sesion.Preparar();
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
        var sesion = SesionConEquipo(out var equipoId, estado);
        var ctx = new Contexto(sesion, equipoId);

        await ctx.Ejecutar();

        ((SesionGrupal)ctx.Actualizada!).Equipos.Should().BeEmpty();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            ctx.SesionId, ctx.EquipoId, It.IsAny<CancellationToken>()), Times.Once);
        // Aviso dirigido a cada integrante del equipo (aquí solo el líder).
        ctx.Notificador.Verify(n => n.NotificarEquipoExpulsadoAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(Lider)),
            ctx.SesionId, ctx.EquipoId, "Rojo", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EquipoConVariosIntegrantes_NotificaTodasLasIdentidades()
    {
        var sesion = SesionConEquipo(out var equipoId);
        sesion.AgregarParticipanteAEquipo(
            equipoId, Integrante, AhoraUtc, AhoraUtc);
        var ctx = new Contexto(sesion, equipoId);

        await ctx.Ejecutar();

        ctx.Notificador.Verify(n => n.NotificarEquipoExpulsadoAsync(
            It.Is<IReadOnlyCollection<Guid>>(ids =>
                ids.Count == 2 &&
                ids.Contains(Lider) &&
                ids.Contains(Integrante)),
            ctx.SesionId, ctx.EquipoId, "Rojo", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OperadorNoDueno_Responde403()
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId, usuarioId: Guid.NewGuid());

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("Participante")]
    [InlineData("Administrador")]
    public async Task RolNoOperador_Responde403(string rol)
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId, rol: rol);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Finalizada)]
    public async Task EstadoNoPermitido_Responde409(EstadoSesion estado)
    {
        var sesion = SesionConEquipo(out var equipoId, estado);
        var ctx = new Contexto(sesion, equipoId);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ExpulsionNoPermitidaExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SesionInexistente_Responde404()
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId);
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task EquipoInexistente_Responde404()
    {
        var sesion = SesionConEquipo(out _);
        var ctx = new Contexto(sesion, Guid.NewGuid());

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<EquipoNoEncontradoExcepcion>();
    }

    [Fact]
    public async Task SesionIndividual_Rechaza()
    {
        var individual = SesionIndividual.Crear(
            "Ind", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 10);
        individual.Preparar();
        var ctx = new Contexto(SesionConEquipo(out var equipoId), equipoId);
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(individual);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<SesionNoGrupalExcepcion>();
    }

    [Fact]
    public async Task SiGuardarFalla_NoNotifica()
    {
        var sesion = SesionConEquipo(out var equipoId);
        var ctx = new Contexto(sesion, equipoId);
        ctx.Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo db"));

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<InvalidOperationException>();
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarEquipoExpulsadoAsync(
            It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<Guid>(),
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
