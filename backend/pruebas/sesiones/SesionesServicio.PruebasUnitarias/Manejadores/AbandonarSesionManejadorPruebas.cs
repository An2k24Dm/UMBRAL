using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.AbandonarSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU48 — Orquestación de AbandonarSesionManejador: solo el propio
// Participante, decide según el tipo de sesión y notifica (solo eventos
// generales, nunca el aviso de expulsado) después de guardar.
public class AbandonarSesionManejadorPruebas
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

        public Contexto(
            Sesion sesion, Guid? usuarioId = null, string rol = "Participante")
        {
            SesionId = sesion.Id;

            Usuario.Setup(u => u.EstaAutenticado()).Returns(true);
            Usuario.Setup(u => u.ObtenerId()).Returns(usuarioId ?? MiembroIdentidad);
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
            Notificador.Setup(n => n.NotificarEquiposSesionActualizadosAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquipoActualizadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarSesionActualizadaAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public AbandonarSesionManejador Construir()
            => new(
                new ValidadorAbandonarSesion(),
                Repo.Object,
                Unidad.Object,
                Usuario.Object,
                Notificador.Object,
                Mock.Of<IRegistroLogsAplicacion>());

        public Task Ejecutar()
            => Construir().Handle(
                new AbandonarSesionComando(SesionId), CancellationToken.None);
    }

    private static SesionIndividual IndividualConParticipante(
        EstadoSesion estado = EstadoSesion.EnPreparacion)
    {
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 5);
        sesion.Preparar();
        sesion.AgregarParticipante(MiembroIdentidad, AhoraUtc);
        if (estado == EstadoSesion.EnPreparacion) return sesion;
        sesion.Iniciar(AhoraUtc);
        if (estado == EstadoSesion.Activa) return sesion;
        if (estado == EstadoSesion.Pausada) { sesion.Pausar(); return sesion; }
        if (estado == EstadoSesion.Finalizada) { sesion.Finalizar(AhoraUtc); return sesion; }
        if (estado == EstadoSesion.Cancelada) { sesion.Cancelar(); return sesion; }
        return sesion;
    }

    private static SesionGrupal GrupalConEquipo(
        out Guid equipoId, bool conMiembro = true)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 5, 3);
        var equipo = sesion.CrearEquipo(
            NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null,
            LiderIdentidad, AhoraUtc, AhoraUtc);
        equipoId = equipo.Id;
        if (conMiembro)
            sesion.AgregarParticipanteAEquipo(
                equipo.Id, MiembroIdentidad, AhoraUtc.AddMinutes(1), AhoraUtc.AddMinutes(1));
        sesion.Preparar();
        return sesion;
    }

    [Fact]
    public async Task Individual_Abandona_GuardaYNotificaParticipantes()
    {
        var ctx = new Contexto(IndividualConParticipante());

        await ctx.Ejecutar();

        ((SesionIndividual)ctx.Actualizada!).Participantes.Should().BeEmpty();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            ctx.SesionId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            ctx.SesionId, EstadoSesion.EnPreparacion.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
        // No es una expulsión: nunca se envía el aviso dirigido.
        ctx.Notificador.Verify(n => n.NotificarParticipanteExpulsadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Grupal_IntegranteAbandona_NotificaEquiposYEquipo()
    {
        var sesion = GrupalConEquipo(out var equipoId);
        var ctx = new Contexto(sesion);

        await ctx.Ejecutar();

        ((SesionGrupal)ctx.Actualizada!).Equipos.Single().Participantes
            .Should().ContainSingle(p => p.ParticipanteIdentidadId == LiderIdentidad);
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            ctx.SesionId, equipoId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            ctx.SesionId, equipoId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            ctx.SesionId, EstadoSesion.EnPreparacion.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
        ctx.Notificador.Verify(n => n.NotificarParticipanteExpulsadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Grupal_LiderAbandona_SeReasignaLiderazgo()
    {
        var sesion = GrupalConEquipo(out var equipoId);
        var miembroSesionId = sesion.Equipos.Single().Participantes
            .Single(p => p.ParticipanteIdentidadId == MiembroIdentidad).Id;
        var ctx = new Contexto(sesion, usuarioId: LiderIdentidad);

        await ctx.Ejecutar();

        var equipo = ((SesionGrupal)ctx.Actualizada!).Equipos.Single();
        equipo.LiderParticipanteId.Should().Be(miembroSesionId);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            ctx.SesionId, equipoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Grupal_LiderUnicoAbandona_EliminaEquipo_YNoNotificaEquipoActualizado()
    {
        var sesion = GrupalConEquipo(out var equipoId, conMiembro: false);
        var ctx = new Contexto(sesion, usuarioId: LiderIdentidad);

        await ctx.Ejecutar();

        ((SesionGrupal)ctx.Actualizada!).Equipos.Should().BeEmpty();
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            ctx.SesionId, equipoId, It.IsAny<CancellationToken>()), Times.Once);
        // El equipo ya no existe: no se lleva a los clientes a su detalle.
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            ctx.SesionId, EstadoSesion.EnPreparacion.ToString(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData("Administrador")]
    [InlineData("Operador")]
    public async Task RolNoParticipante_Rechaza(string rol)
    {
        var ctx = new Contexto(IndividualConParticipante(), rol: rol);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task NoAutenticado_Rechaza()
    {
        var ctx = new Contexto(IndividualConParticipante());
        ctx.Usuario.Setup(u => u.EstaAutenticado()).Returns(false);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<AccesoSesionNoPermitidoExcepcion>();
    }

    [Fact]
    public async Task SesionInexistente_Responde404()
    {
        var ctx = new Contexto(IndividualConParticipante());
        ctx.Repo.Setup(r => r.ObtenerPorIdAsync(ctx.SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<SesionNoEncontradaExcepcion>();
    }

    [Fact]
    public async Task NoInscrito_Responde404()
    {
        var ctx = new Contexto(IndividualConParticipante(), usuarioId: Guid.NewGuid());

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ParticipanteNoEncontradoExcepcion>();
    }

    [Theory]
    [InlineData(EstadoSesion.Activa)]
    [InlineData(EstadoSesion.Pausada)]
    [InlineData(EstadoSesion.Finalizada)]
    [InlineData(EstadoSesion.Cancelada)]
    public async Task Individual_EstadoNoPermitido_Responde409(EstadoSesion estado)
    {
        var ctx = new Contexto(IndividualConParticipante(estado));

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ParticipacionInvalidaExcepcion>();
        ctx.Unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Individual_Programada_Responde409()
    {
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "ABC123", Operador, AhoraUtc, 5);
        var ctx = new Contexto(sesion);

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<ParticipacionInvalidaExcepcion>();
    }

    [Fact]
    public async Task SiGuardarFalla_NoNotifica()
    {
        var ctx = new Contexto(IndividualConParticipante());
        ctx.Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo db"));

        Func<Task> accion = () => ctx.Ejecutar();

        await accion.Should().ThrowAsync<InvalidOperationException>();
        ctx.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarEquiposSesionActualizadosAsync(
            It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
