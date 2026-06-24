using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.EliminarEquipo;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU42 — Orquestación de EliminarEquipoManejador: rol Participante, líder y
// persistencia. La regla de líder/estado vive en el dominio.
public class EliminarEquipoManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 6, 23, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid Lider = Guid.Parse("55555555-5555-5555-5555-555555555555");

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
            Sesion? sesion = null, Guid? equipoId = null,
            Guid? usuarioId = null, string rol = "Participante")
        {
            var s = sesion ?? SesionConEquipo(Lider, out _);
            SesionId = s.Id;
            EquipoId = equipoId ?? (s is SesionGrupal g && g.Equipos.Any()
                ? g.Equipos.First().Id : Guid.NewGuid());

            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(usuarioId ?? Lider);
            Usuario.Setup(u => u.TieneAlgunRol(It.IsAny<string[]>()))
                .Returns<string[]>(roles => Array.IndexOf(roles, rol) >= 0);

            Repo.Setup(r => r.ObtenerPorIdAsync(SesionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(s);
            Repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Callback<Sesion, CancellationToken>((x, _) => Actualizada = x)
                .Returns(Task.CompletedTask);
            Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquiposSesionActualizadosAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public EliminarEquipoManejador Construir()
            => new(Repo.Object, Unidad.Object, Usuario.Object, Notificador.Object);
    }

    private static SesionGrupal SesionConEquipo(Guid lider, out Guid equipoId)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 3);
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, lider, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;
        sesion.Preparar();
        return sesion;
    }

    private static SesionGrupal SesionEnEstado(EstadoSesion estado, out Guid equipoId)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 5, 3);
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null, Lider, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;
        // Programada -> EnPreparacion -> (Activa) -> (Pausada) ...
        if (estado == EstadoSesion.Programada) return sesion;
        sesion.Preparar();
        if (estado == EstadoSesion.EnPreparacion) return sesion;
        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Activa) return sesion;
        if (estado == EstadoSesion.Pausada) { sesion.Pausar(); return sesion; }
        if (estado == EstadoSesion.Finalizada) { sesion.Finalizar(AhoraUtc); return sesion; }
        if (estado == EstadoSesion.Cancelada)
        {
            // Cancelar desde Activa.
            sesion.Cancelar();
            return sesion;
        }
        return sesion;
    }

    [Fact]
    public async Task Lider_EliminaEquipo_GuardaCambios()
    {
        var ctx = new Contexto();

        await ctx.Construir().Handle(
            new EliminarEquipoComando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);

        ((SesionGrupal)ctx.Actualizada!).Equipos.Should().BeEmpty();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            ctx.SesionId, ctx.EquipoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoLider_Responde403()
    {
        var ctx = new Contexto(usuarioId: Guid.NewGuid());

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarEquipoComando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task Operador_NoPuedeEliminar()
    {
        var ctx = new Contexto(rol: "Operador");

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarEquipoComando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task Administrador_NoPuedeEliminar()
    {
        var ctx = new Contexto(rol: "Administrador");

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarEquipoComando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task SesionInexistente_Responde404()
    {
        var ctx = new Contexto();
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarEquipoComando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task EquipoInexistente_Responde404()
    {
        var ctx = new Contexto();

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarEquipoComando(ctx.SesionId, Guid.NewGuid()), CancellationToken.None);
        await accion.Should().ThrowAsync<EquipoNoEncontradoExcepcion>();
    }

    [Fact]
    public async Task SesionIndividual_Rechaza()
    {
        var individual = SesionIndividual.Crear(
            "Ind", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 10);
        var ctx = new Contexto(individual, equipoId: Guid.NewGuid());

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarEquipoComando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<SesionNoGrupalExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task SesionNoEnPreparacion_Rechaza(EstadoSesion estado)
    {
        var sesion = SesionEnEstado(estado, out var equipoId);
        var ctx = new Contexto(sesion, equipoId);

        Func<Task> accion = () => ctx.Construir().Handle(
            new EliminarEquipoComando(ctx.SesionId, ctx.EquipoId), CancellationToken.None);
        await accion.Should().ThrowAsync<EquipoInvalidoExcepcion>();
    }
}
