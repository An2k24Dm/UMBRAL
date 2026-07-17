using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Procesos.VencimientoEtapas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Procesos;

public class ProcesadorVencimientoEtapasSesionPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MisionId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static SesionIndividual SesionConEtapa(Guid etapaId)
    {
        var s = SesionIndividual.Crear(
            "Vencimiento", "Demo", AhoraUtc.AddHours(1), "VEN001", Operador, AhoraUtc, 5);
        s.AsignarMisiones(new List<Guid> { MisionId });
        s.Preparar();
        s.IniciarPrimeraEtapa(MisionId, etapaId, Guid.NewGuid(), "Trivia", 1, AhoraUtc, 60);
        return s;
    }

    // Sesión Activa cuya EjecucionActual quedó en Preparacion apuntando a etapaProgramadaId.
    private static SesionIndividual SesionConPreparacion(Guid etapaProgramadaId)
    {
        var etapaActualId = Guid.NewGuid();
        var s = SesionIndividual.Crear(
            "Preparacion", "Demo", AhoraUtc.AddHours(1), "PREP01", Operador, AhoraUtc, 5);
        s.AsignarMisiones(new List<Guid> { MisionId });
        s.Preparar();
        s.IniciarPrimeraEtapa(MisionId, etapaActualId, Guid.NewGuid(), "Trivia", 1, AhoraUtc, 60);
        s.ProgramarSiguienteEtapa(
            etapaActualId,
            EjecucionActualSesion.Planificar(
                MisionId, etapaProgramadaId, Guid.NewGuid(), "Trivia", 2, 1, 2, 60),
            AhoraUtc,
            10);
        return s;
    }

    // Sesión Activa cuya EjecucionActual quedó en CierrePendiente (feedback final).
    private static SesionIndividual SesionConCierrePendiente(Guid etapaId)
    {
        var s = SesionIndividual.Crear(
            "Cierre", "Demo", AhoraUtc.AddHours(1), "CIE001", Operador, AhoraUtc, 5);
        s.AsignarMisiones(new List<Guid> { MisionId });
        s.Preparar();
        s.IniciarPrimeraEtapa(MisionId, etapaId, Guid.NewGuid(), "Trivia", 1, AhoraUtc, 60);
        s.ProgramarCierrePendiente(etapaId, AhoraUtc, 5);
        return s;
    }

    private sealed class Arranque
    {
        public Mock<IConsultasSesiones> Consultas { get; } = new();
        public Mock<IServicioFinalizacionSesion> Finalizacion { get; } = new();
        public Mock<IProveedorFechaHora> Reloj { get; } = new();
        public Mock<IRegistroLogsAplicacion> Log { get; } = new();

        public Arranque()
        {
            Reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc.AddSeconds(61));
            Consultas.Setup(c => c.ListarActivasConEtapaVencidaAsync(
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Sesion>());
            Consultas.Setup(c => c.ListarActivasConPreparacionVencidaAsync(
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Sesion>());
            Consultas.Setup(c => c.ListarActivasConCierrePendienteVencidoAsync(
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Sesion>());
            Consultas.Setup(c => c.ListarActivasConDuracionVencidaAsync(
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Array.Empty<Sesion>());
            Finalizacion.Setup(f => f.AvanzarEtapaPorVencimientoAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Finalizacion.Setup(f => f.FinalizarSesionPorVencimientoAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Finalizacion.Setup(f => f.ActivarEtapaProgramadaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Finalizacion.Setup(f => f.CerrarEtapaTrasCierrePendienteAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        public Arranque ConVencidas(params Sesion[] sesiones)
        {
            Consultas.Setup(c => c.ListarActivasConEtapaVencidaAsync(
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesiones);
            return this;
        }

        public Arranque ConSesionesVencidas(params Sesion[] sesiones)
        {
            Consultas.Setup(c => c.ListarActivasConDuracionVencidaAsync(
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesiones);
            return this;
        }

        public Arranque ConPreparadas(params Sesion[] sesiones)
        {
            Consultas.Setup(c => c.ListarActivasConPreparacionVencidaAsync(
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesiones);
            return this;
        }

        public Arranque ConCierresPendientes(params Sesion[] sesiones)
        {
            Consultas.Setup(c => c.ListarActivasConCierrePendienteVencidoAsync(
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesiones);
            return this;
        }

        public ProcesadorVencimientoEtapasSesion Construir()
            => new(Consultas.Object, Finalizacion.Object, Reloj.Object, Log.Object);
    }

    [Fact]
    public async Task SinTransiciones_NoLlamaNada()
    {
        var arr = new Arranque();

        var procesadas = await arr.Construir().EjecutarCicloAsync(CancellationToken.None);

        procesadas.Should().Be(0);
        arr.Finalizacion.Verify(f => f.AvanzarEtapaPorVencimientoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        arr.Finalizacion.Verify(f => f.ActivarEtapaProgramadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        arr.Finalizacion.Verify(f => f.FinalizarSesionPorVencimientoAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConSesionVencida_FinalizaLaSesion()
    {
        var etapaId = Guid.NewGuid();
        var sesion = SesionConEtapa(etapaId);
        var arr = new Arranque().ConSesionesVencidas(sesion);

        var procesadas = await arr.Construir().EjecutarCicloAsync(CancellationToken.None);

        procesadas.Should().Be(1);
        arr.Finalizacion.Verify(f => f.FinalizarSesionPorVencimientoAsync(
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
        arr.Finalizacion.Verify(f => f.AvanzarEtapaPorVencimientoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ConEtapaVencida_DisparaCierrePorVencimientoDeEsaEtapa()
    {
        var etapaId = Guid.NewGuid();
        var sesion = SesionConEtapa(etapaId);
        var arr = new Arranque().ConVencidas(sesion);

        var procesadas = await arr.Construir().EjecutarCicloAsync(CancellationToken.None);

        procesadas.Should().Be(1);
        arr.Finalizacion.Verify(f => f.AvanzarEtapaPorVencimientoAsync(
            sesion.Id, etapaId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // #14/#15: cierre pendiente vencido → cierra la etapa exactamente una vez.
    public async Task ConCierrePendienteVencido_CierraLaEtapa()
    {
        var etapaId = Guid.NewGuid();
        var sesion = SesionConCierrePendiente(etapaId);
        var arr = new Arranque().ConCierresPendientes(sesion);

        var procesadas = await arr.Construir().EjecutarCicloAsync(CancellationToken.None);

        procesadas.Should().Be(1);
        arr.Finalizacion.Verify(f => f.CerrarEtapaTrasCierrePendienteAsync(
            sesion.Id, etapaId, It.IsAny<CancellationToken>()), Times.Once);
        arr.Finalizacion.Verify(f => f.AvanzarEtapaPorVencimientoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // #32: preparación vencida → activa exactamente una vez.
    public async Task ConPreparacionVencida_ActivaLaEtapaProgramada()
    {
        var etapaProgramadaId = Guid.NewGuid();
        var sesion = SesionConPreparacion(etapaProgramadaId);
        var arr = new Arranque().ConPreparadas(sesion);

        var procesadas = await arr.Construir().EjecutarCicloAsync(CancellationToken.None);

        procesadas.Should().Be(1);
        arr.Finalizacion.Verify(f => f.ActivarEtapaProgramadaAsync(
            sesion.Id, etapaProgramadaId, It.IsAny<CancellationToken>()), Times.Once);
        arr.Finalizacion.Verify(f => f.AvanzarEtapaPorVencimientoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ErrorEnUnaSesion_NoDetieneLasDemas()
    {
        var etapa1 = Guid.NewGuid();
        var etapa2 = Guid.NewGuid();
        var sesion1 = SesionConEtapa(etapa1);
        var sesion2 = SesionConEtapa(etapa2);
        var arr = new Arranque().ConVencidas(sesion1, sesion2);
        arr.Finalizacion.Setup(f => f.AvanzarEtapaPorVencimientoAsync(
                sesion1.Id, etapa1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo transitorio"));

        var procesadas = await arr.Construir().EjecutarCicloAsync(CancellationToken.None);

        procesadas.Should().Be(1); // la segunda se procesa igual
        arr.Finalizacion.Verify(f => f.AvanzarEtapaPorVencimientoAsync(
            sesion2.Id, etapa2, It.IsAny<CancellationToken>()), Times.Once);
    }
}
