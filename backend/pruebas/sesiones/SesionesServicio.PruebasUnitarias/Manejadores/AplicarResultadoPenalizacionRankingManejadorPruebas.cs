using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.AplicarResultadoPenalizacionRanking;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.PruebasUnitarias.Dominio; // EquipoTestHelpers (CrearEquipo de 4 args)

namespace SesionesServicio.PruebasUnitarias.Manejadores;

// HU52 — Orquestación de AplicarResultadoPenalizacionRankingManejador: actualiza
// el snapshot autoritativo, marca la penalización Procesada e idempotencia.
public sealed class AplicarResultadoPenalizacionRankingManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> Repo { get; } = new();
        public Mock<IRepositorioPenalizacionesSesion> RepoPen { get; } = new();
        public Mock<IUnidadTrabajoSesiones> Unidad { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();

        public Contexto(Sesion? sesion, PenalizacionSesion? penalizacion)
        {
            Repo.Setup(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Repo.Setup(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            RepoPen.Setup(r => r.ObtenerPorEventoIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(penalizacion);
            RepoPen.Setup(r => r.ActualizarAsync(It.IsAny<PenalizacionSesion>(), It.IsAny<CancellationToken>()))
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
            => new(Repo.Object, RepoPen.Object, Unidad.Object, Notificador.Object,
                Reloj.Object, Mock.Of<IRegistroLogsAplicacion>());
    }

    private static SesionIndividual CrearSesionIndividual(out Guid participanteSesionId)
    {
        var sesion = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "IND123", Operador, AhoraUtc, 5);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });
        sesion.Preparar();
        participanteSesionId = sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc).Id;
        sesion.Iniciar(AhoraUtc);
        return sesion;
    }

    private static SesionGrupal CrearSesionGrupal(out Guid equipoId)
    {
        var sesion = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "GRP123", Operador, AhoraUtc, 3, 3);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });
        sesion.Preparar();
        equipoId = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc).Id;
        sesion.Iniciar(AhoraUtc);
        return sesion;
    }

    private static AplicarResultadoPenalizacionRankingComando ComandoIndividual(
        Guid eventoId, Guid sesionId, Guid participanteSesionId,
        int acumulado, long total)
        => new(eventoId, Guid.NewGuid(), sesionId, "Participante",
            participanteSesionId, Guid.NewGuid(), null,
            5, acumulado, total, null, AhoraUtc);

    private static AplicarResultadoPenalizacionRankingComando ComandoEquipo(
        Guid eventoId, Guid sesionId, Guid equipoId, int acumulado, long total)
        => new(eventoId, Guid.NewGuid(), sesionId, "Equipo",
            null, null, equipoId,
            10, acumulado, null, total, AhoraUtc);

    [Fact]
    public async Task ResultadoIndividual_actualizaSnapshotYMarcaProcesada()
    {
        var sesion = CrearSesionIndividual(out var pid);
        var penalizacion = PenalizacionSesion.CrearParaParticipante(
            Guid.NewGuid(), sesion.Id, pid, Guid.NewGuid(), 5, "Motivo", Operador, AhoraUtc);
        var ctx = new Contexto(sesion, penalizacion);
        var comando = ComandoIndividual(penalizacion.EventoId, sesion.Id, pid, acumulado: 5, total: -3);

        await ctx.Construir().Handle(comando, CancellationToken.None);

        var participante = sesion.Participantes.Single(p => p.Id == pid);
        participante.Puntaje.Valor.Should().Be(-3);
        participante.PuntosPenalizados.Should().Be(5);
        penalizacion.EstadoProcesamiento.Should().Be(EstadoProcesamientoPenalizacion.Procesada);
        penalizacion.PuntajeResultante.Should().Be(-3);
        ctx.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResultadoGrupal_actualizaSnapshotEquipoYNotifica()
    {
        var sesion = CrearSesionGrupal(out var equipoId);
        var penalizacion = PenalizacionSesion.CrearParaEquipo(
            Guid.NewGuid(), sesion.Id, equipoId, 10, "Motivo", Operador, AhoraUtc);
        var ctx = new Contexto(sesion, penalizacion);
        var comando = ComandoEquipo(penalizacion.EventoId, sesion.Id, equipoId, acumulado: 20, total: 60);

        await ctx.Construir().Handle(comando, CancellationToken.None);

        var equipo = sesion.Equipos.Single(e => e.Id == equipoId);
        equipo.Puntaje.Valor.Should().Be(60);
        equipo.PuntosPenalizados.Should().Be(20);
        penalizacion.EstadoProcesamiento.Should().Be(EstadoProcesamientoPenalizacion.Procesada);
        ctx.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            sesion.Id, equipoId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResultadoDuplicado_yaProcesado_noReaplicaNiNotifica()
    {
        var sesion = CrearSesionIndividual(out var pid);
        var penalizacion = PenalizacionSesion.CrearParaParticipante(
            Guid.NewGuid(), sesion.Id, pid, Guid.NewGuid(), 5, "Motivo", Operador, AhoraUtc);
        penalizacion.MarcarProcesada(-3, AhoraUtc); // ya procesada
        var ctx = new Contexto(sesion, penalizacion);
        var comando = ComandoIndividual(penalizacion.EventoId, sesion.Id, pid, acumulado: 5, total: -3);

        await ctx.Construir().Handle(comando, CancellationToken.None);

        sesion.Participantes.Single(p => p.Id == pid).PuntosPenalizados.Should().Be(0);
        ctx.Repo.Verify(r => r.ActualizarAsync(It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SesionInexistente_noHaceNada()
    {
        var penalizacion = PenalizacionSesion.CrearParaEquipo(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, "Motivo", Operador, AhoraUtc);
        var ctx = new Contexto(sesion: null, penalizacion);
        var comando = ComandoEquipo(penalizacion.EventoId, Guid.NewGuid(), Guid.NewGuid(), 10, 5);

        await ctx.Construir().Handle(comando, CancellationToken.None);

        ctx.Notificador.VerifyNoOtherCalls();
    }
}
