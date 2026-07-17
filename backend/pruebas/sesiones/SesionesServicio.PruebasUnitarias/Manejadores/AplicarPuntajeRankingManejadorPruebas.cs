using System;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Comandos.AplicarPuntajeRanking;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.PruebasUnitarias.Dominio;

namespace SesionesServicio.PruebasUnitarias.Manejadores;

public class AplicarPuntajeRankingManejadorPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 16, 14, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly Guid EventoOrigen = Guid.Parse("66666666-6666-6666-6666-666666666666");

    [Fact]
    public async Task SesionNoEncontrada_ActualizaCorrelacionPeroNoNotifica()
    {
        var arranque = new Arranque(null);
        var comando = CrearComando(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null);

        await arranque.Manejador.Handle(comando, CancellationToken.None);

        arranque.Respuestas.Verify(r => r.ActualizarPuntosGanadosPorEventoAsync(
            EventoOrigen, 25, It.IsAny<CancellationToken>()), Times.Once);
        arranque.Evidencias.Verify(r => r.ActualizarPuntosGanadosPorEventoAsync(
            EventoOrigen, 25, It.IsAny<CancellationToken>()), Times.Once);
        arranque.Repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        arranque.ResultadosProcesados.Verify(r => r.RegistrarAsync(
            EventoOrigen,
            "ranking.puntaje_actualizado",
            AhoraUtc,
            It.IsAny<CancellationToken>()), Times.Once);
        arranque.Notificador.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SesionIndividual_AplicaSnapshotReciente_YNotificaParticipantes()
    {
        var (sesion, participante) = CrearSesionIndividual();
        var arranque = new Arranque(sesion);
        var comando = CrearComando(
            sesion.Id, participante.Id, participante.ParticipanteIdentidadId, null);

        await arranque.Manejador.Handle(comando, CancellationToken.None);

        participante.Puntaje.Valor.Should().Be(100);
        participante.SnapshotRankingUtc.Should().Be(AhoraUtc);
        arranque.Repositorio.Verify(r => r.ActualizarAsync(
            sesion, It.IsAny<CancellationToken>()), Times.Once);
        arranque.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
        arranque.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ResultadoDuplicado_yaProcesado_noActualizaNiNotifica()
    {
        var (sesion, participante) = CrearSesionIndividual();
        var arranque = new Arranque(sesion);
        arranque.ResultadosProcesados.Setup(r => r.ExisteAsync(
                EventoOrigen,
                "ranking.puntaje_actualizado",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var comando = CrearComando(
            sesion.Id, participante.Id, participante.ParticipanteIdentidadId, null);

        await arranque.Manejador.Handle(comando, CancellationToken.None);

        arranque.Respuestas.Verify(r => r.ActualizarPuntosGanadosPorEventoAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        arranque.Evidencias.Verify(r => r.ActualizarPuntosGanadosPorEventoAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        arranque.Repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        arranque.Notificador.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SesionIndividual_SnapshotAntiguo_NoPersisteNiNotifica()
    {
        var (sesion, participante) = CrearSesionIndividual();
        participante.EstablecerPuntajeSnapshot(150, AhoraUtc);
        var arranque = new Arranque(sesion);
        var comando = CrearComando(
            sesion.Id,
            participante.Id,
            participante.ParticipanteIdentidadId,
            null,
            calculadoEnUtc: AhoraUtc.AddSeconds(-5));

        await arranque.Manejador.Handle(comando, CancellationToken.None);

        participante.Puntaje.Valor.Should().Be(150);
        arranque.Repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        arranque.Notificador.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SesionGrupal_AplicaSnapshotEquipo_YNotificaEquipoYParticipantes()
    {
        var (sesion, equipo, participante) = CrearSesionGrupal();
        var arranque = new Arranque(sesion);
        var comando = CrearComando(
            sesion.Id,
            participante.Id,
            participante.ParticipanteIdentidadId,
            equipo.Id,
            puntajeTotalParticipante: 80,
            puntajeTotalEquipo: 210);

        await arranque.Manejador.Handle(comando, CancellationToken.None);

        participante.Puntaje.Valor.Should().Be(80);
        equipo.Puntaje.Valor.Should().Be(210);
        equipo.SnapshotRankingUtc.Should().Be(AhoraUtc);
        arranque.Repositorio.Verify(r => r.ActualizarAsync(
            sesion, It.IsAny<CancellationToken>()), Times.Once);
        arranque.Notificador.Verify(n => n.NotificarEquipoActualizadoAsync(
            sesion.Id, equipo.Id, It.IsAny<CancellationToken>()), Times.Once);
        arranque.Notificador.Verify(n => n.NotificarParticipantesSesionActualizadosAsync(
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SesionGrupal_SinEquipoId_NoAplicaSnapshot()
    {
        var (sesion, _, participante) = CrearSesionGrupal();
        var arranque = new Arranque(sesion);
        var comando = CrearComando(
            sesion.Id, participante.Id, participante.ParticipanteIdentidadId, null);

        await arranque.Manejador.Handle(comando, CancellationToken.None);

        participante.Puntaje.Valor.Should().Be(0);
        arranque.Repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        arranque.Notificador.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SesionGrupal_EquipoInexistente_NoAplicaSnapshot()
    {
        var (sesion, _, participante) = CrearSesionGrupal();
        var arranque = new Arranque(sesion);
        var comando = CrearComando(
            sesion.Id, participante.Id, participante.ParticipanteIdentidadId, Guid.NewGuid());

        await arranque.Manejador.Handle(comando, CancellationToken.None);

        participante.Puntaje.Valor.Should().Be(0);
        arranque.Repositorio.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        arranque.Notificador.VerifyNoOtherCalls();
    }

    private static AplicarPuntajeRankingComando CrearComando(
        Guid sesionId,
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        long puntajeGanado = 25,
        long puntajeTotalParticipante = 100,
        long? puntajeTotalEquipo = null,
        DateTime? calculadoEnUtc = null)
        => new(
            EventoOrigen,
            sesionId,
            participanteSesionId,
            participanteIdentidadId,
            equipoId,
            puntajeGanado,
            puntajeTotalParticipante,
            puntajeTotalEquipo,
            calculadoEnUtc ?? AhoraUtc);

    private static (SesionIndividual Sesion, Participante Participante) CrearSesionIndividual()
    {
        var sesion = SesionIndividual.Crear(
            "Sesión",
            "Demo",
            AhoraUtc.AddHours(1),
            "IND123",
            Operador,
            AhoraUtc,
            maximoParticipantes: 5);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });
        sesion.Preparar();
        var participante = sesion.AgregarParticipante(Guid.NewGuid(), AhoraUtc);
        return (sesion, participante);
    }

    private static (SesionGrupal Sesion, Equipo Equipo, Participante Participante) CrearSesionGrupal()
    {
        var sesion = SesionGrupal.Crear(
            "Sesión",
            "Demo",
            AhoraUtc.AddHours(1),
            "GRP123",
            Operador,
            AhoraUtc,
            maximoEquipos: 3,
            maximoParticipantesPorEquipo: 3);
        sesion.AsignarMisiones(new[] { Guid.NewGuid() });
        sesion.Preparar();
        var equipo = sesion.CrearEquipo("Rojo", Guid.NewGuid(), AhoraUtc, AhoraUtc);
        var participante = equipo.Participantes[0];
        return (sesion, equipo, participante);
    }

    private sealed class Arranque
    {
        public Mock<IRepositorioSesiones> Repositorio { get; } = new();
        public Mock<IRepositorioRespuestasTrivia> Respuestas { get; } = new();
        public Mock<IRepositorioEvidenciasTesoro> Evidencias { get; } = new();
        public Mock<IRepositorioResultadosRankingProcesados> ResultadosProcesados { get; } = new();
        public Mock<IUnidadTrabajoSesiones> UnidadTrabajo { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public AplicarPuntajeRankingManejador Manejador { get; }

        public Arranque(Sesion? sesion)
        {
            Repositorio.Setup(r => r.ObtenerPorIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);
            Respuestas.Setup(r => r.ActualizarPuntosGanadosPorEventoAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);
            Evidencias.Setup(r => r.ActualizarPuntosGanadosPorEventoAsync(
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);
            ResultadosProcesados.Setup(r => r.ExisteAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);
            ResultadosProcesados.Setup(r => r.RegistrarAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Repositorio.Setup(r => r.ActualizarAsync(
                    It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarParticipantesSesionActualizadosAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEquipoActualizadoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            UnidadTrabajo.Setup(u => u.EjecutarEnTransaccionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));

            Manejador = new AplicarPuntajeRankingManejador(
                Repositorio.Object,
                Respuestas.Object,
                Evidencias.Object,
                ResultadosProcesados.Object,
                UnidadTrabajo.Object,
                Notificador.Object);
        }
    }
}
