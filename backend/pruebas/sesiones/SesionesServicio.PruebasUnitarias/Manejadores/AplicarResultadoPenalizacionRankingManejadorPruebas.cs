using SesionesServicio.Aplicacion.Comandos.AplicarResultadoPenalizacionRanking;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.PruebasUnitarias.Dominio;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

public sealed class AplicarResultadoPenalizacionRankingManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IRepositorioResultadosRankingProcesados> RepoResultados { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();

        public Contexto(Sesion? sesion, bool yaProcesado = false)
        {
            Repo.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            RepoResultados.Setup(r => r.ExisteAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(yaProcesado);
            RepoResultados.Setup(r => r.RegistrarAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
            Unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Unidad.Setup(u => u.EjecutarEnTransaccionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));
            Notificador.Setup(n => n.NotificarParticipantesSesionActualizadosAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquipoActualizadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        }

        public AplicarResultadoPenalizacionRankingManejador Construir()
            => new(Repo.Object, RepoResultados.Object, Unidad.Object, Notificador.Object,
                Reloj.Object, Mock.Of<IRegistroLogsAplicacion>());
    }

    private static SesionIndividual CrearSesionIndividual(out Guid participanteSesionId)
    {
        var sesion = SesionIndividual.Crear(
            "Sesion", "Demo", AhoraUtc.AddHours(1), "IND123", Operador, AhoraUtc, 5);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });
        sesion.Preparar();
        participanteSesionId = sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc).Id;
        sesion.Iniciar(AhoraUtc);
        return sesion;
    }

    private static SesionGrupal CrearSesionGrupal(out Guid equipoId)
    {
        var sesion = SesionGrupal.Crear(
            "Sesion", "Demo", AhoraUtc.AddHours(1), "GRP123", Operador, AhoraUtc, 3, 3);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });
        sesion.Preparar();
        equipoId = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc).Id;
        sesion.Iniciar(AhoraUtc);
        return sesion;
    }

    private static AplicarResultadoPenalizacionRankingComando ComandoIndividual(
        Guid eventoId, Guid sesionId, Guid participanteSesionId,
        int acumulado, long total)
        => new(eventoId, sesionId, "Participante",
            participanteSesionId, Guid.NewGuid(), null,
            5, acumulado, total, null, AhoraUtc);

    private static AplicarResultadoPenalizacionRankingComando ComandoEquipo(
        Guid eventoId, Guid sesionId, Guid equipoId, int acumulado, long total)
        => new(eventoId, sesionId, "Equipo",
            null, null, equipoId,
            10, acumulado, null, total, AhoraUtc);

    [Fact]
    public async Task ResultadoIndividual_actualizaSnapshotYRegistraInbox()
    {
        var sesion = CrearSesionIndividual(out var pid);
        var eventoId = Guid.NewGuid();
        var ctx = new Contexto(sesion);

        await ctx.Construir().Handle(
            ComandoIndividual(eventoId, sesion.Id, pid, acumulado: 5, total: -3),
            CancellationToken.None);

        var participante = sesion.Participantes.Single(p => p.Id == pid);
        participante.Puntaje.Valor.Should().Be(-3);
        participante.PuntosPenalizados.Should().Be(5);
        ctx.RepoResultados.Verify(r => r.RegistrarAsync(
            eventoId, "ranking.penalizacion_procesada", AhoraUtc,
            It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResultadoGrupal_actualizaSnapshotEquipoYNotifica()
    {
        var sesion = CrearSesionGrupal(out var equipoId);
        var eventoId = Guid.NewGuid();
        var ctx = new Contexto(sesion);

        await ctx.Construir().Handle(
            ComandoEquipo(eventoId, sesion.Id, equipoId, acumulado: 20, total: 60),
            CancellationToken.None);

        var equipo = sesion.Equipos.Single(e => e.Id == equipoId);
        equipo.Puntaje.Valor.Should().Be(60);
        equipo.PuntosPenalizados.Should().Be(20);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            sesion.Id, equipoId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResultadoDuplicado_yaProcesado_noReaplicaNiNotifica()
    {
        var sesion = CrearSesionIndividual(out var pid);
        var eventoId = Guid.NewGuid();
        var ctx = new Contexto(sesion, yaProcesado: true);

        await ctx.Construir().Handle(
            ComandoIndividual(eventoId, sesion.Id, pid, acumulado: 5, total: -3),
            CancellationToken.None);

        sesion.Participantes.Single(p => p.Id == pid).PuntosPenalizados.Should().Be(0);
        ctx.Repo.Verify(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SesionInexistente_noHaceNada()
    {
        var ctx = new Contexto(sesion: null);

        await ctx.Construir().Handle(
            ComandoEquipo(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, 5),
            CancellationToken.None);

        ctx.Notificador.VerifyNoOtherCalls();
        ctx.RepoResultados.Verify(r => r.RegistrarAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
