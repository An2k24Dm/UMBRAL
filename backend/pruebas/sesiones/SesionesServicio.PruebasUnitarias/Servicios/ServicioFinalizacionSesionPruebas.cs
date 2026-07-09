using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Servicios;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Servicios;

public class ServicioFinalizacionSesionPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid MisionId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid EtapaId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static SesionIndividual SesionActiva()
    {
        var s = SesionIndividual.Crear(
            "Finalizacion", "Demo", AhoraUtc.AddHours(1), "FIN001", Operador, AhoraUtc, 5);
        s.AsignarMisiones(new List<Guid> { MisionId });
        s.Preparar();
        s.IniciarPrimeraEtapa(MisionId, EtapaId, Guid.NewGuid(), "Trivia", 1, AhoraUtc, 60);
        return s;
    }

    private sealed class Contexto
    {
        public Mock<IRepositorioSesiones> RepoSesiones { get; } = new();
        public Mock<IRepositorioEtapasCompletadas> RepoEtapas { get; } = new();
        public Mock<IClienteJuegosMisiones> ClienteMisiones { get; } = new();
        public Mock<INotificadorSesionesTiempoReal> Notificador { get; } = new();
        public Mock<IUnidadTrabajoSesiones> UnidadTrabajo { get; } = new();
        public Sesion? SesionActualizada;
        private readonly DateTime _ahora;

        public Contexto(
            Sesion? sesion,
            int totalEtapasPorMision = 1,
            int etapasCompletadas = 1,
            DateTime? ahora = null)
        {
            _ahora = ahora ?? AhoraUtc;
            RepoSesiones.Setup(r => r.ObtenerPorIdAsync(
                    It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(sesion);

            RepoSesiones.Setup(r => r.ActualizarAsync(
                    It.IsAny<Sesion>(), It.IsAny<CancellationToken>()))
                .Callback<Sesion, CancellationToken>((s, _) => SesionActualizada = s)
                .Returns(Task.CompletedTask);

            RepoEtapas.Setup(r => r.RegistrarAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(),
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            RepoEtapas.Setup(r => r.ContarAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(etapasCompletadas);

            ClienteMisiones.Setup(c => c.ObtenerMisionConEtapasAsync(
                    MisionId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MisionConEtapasJuegosDto
                {
                    Id = MisionId,
                    Etapas = BuildEtapas(totalEtapasPorMision)
                });

            Notificador.Setup(n => n.NotificarSesionActualizadaAsync(
                    It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Notificador.Setup(n => n.NotificarEtapaIniciadaAsync(
                    It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
                    It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            UnidadTrabajo.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            UnidadTrabajo.Setup(u => u.EjecutarEnTransaccionAsync(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));
        }

        public ServicioFinalizacionSesion Construir()
            => new(
                RepoSesiones.Object,
                RepoEtapas.Object,
                ClienteMisiones.Object,
                Notificador.Object,
                UnidadTrabajo.Object,
                BuildReloj());

        public Task Ejecutar(Guid? sesionId = null)
            => Construir().FinalizarSiTodasEtapasCompletadasAsync(
                sesionId ?? SesionId(), EtapaId, CancellationToken.None);

        private Guid SesionId() => Guid.NewGuid();

        private IProveedorFechaHora BuildReloj()
        {
            var r = new Mock<IProveedorFechaHora>();
            r.Setup(x => x.ObtenerFechaHoraUtc()).Returns(_ahora);
            return r.Object;
        }

        private static List<EtapaJuegosDto> BuildEtapas(int count)
        {
            var list = new List<EtapaJuegosDto>();
            for (var i = 0; i < count; i++)
                list.Add(new EtapaJuegosDto
                {
                    Id = i == 0 ? EtapaId : Guid.NewGuid(),
                    Orden = i + 1,
                    TipoModoDeJuego = "Trivia",
                    ModoDeJuegoId = Guid.NewGuid(),
                    TiempoEstimado = 60
                });
            return list;
        }
    }

    [Fact]
    public async Task SesionNoExiste_NoFinaliza_NoLanzaExcepcion()
    {
        var ctx = new Contexto(sesion: null);
        var sesionId = Guid.NewGuid();

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesionId, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SesionNoActiva_NoFinaliza()
    {
        var sesion = SesionIndividual.Crear(
            "Finalizacion", "Demo", AhoraUtc.AddHours(1), "FIN001", Operador, AhoraUtc, 5);
        sesion.AsignarMisiones(new List<Guid> { MisionId });
        sesion.Preparar(); // EnPreparacion, no Activa
        var ctx = new Contexto(sesion);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TodasEtapasCompletadas_FinalizaSesion()
    {
        var sesion = SesionActiva();
        // 1 mision con 1 etapa, 1 etapa completada → debe finalizar
        var ctx = new Contexto(sesion, totalEtapasPorMision: 1, etapasCompletadas: 1);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.SesionActualizada.Should().NotBeNull();
        ctx.SesionActualizada!.Estado.Should().Be(EstadoSesion.Finalizada);
        ctx.SesionActualizada.FechaFinalizacionUtc.Should().Be(AhoraUtc);
    }

    [Fact]
    public async Task TodasEtapasCompletadas_GuardaCambiosYNotifica()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 1, etapasCompletadas: 1);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.UnidadTrabajo.Verify(u => u.GuardarCambiosAsync(
            It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarSesionActualizadaAsync(
            sesion.Id, EstadoSesion.Finalizada.ToString(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoTodasEtapasCompletadas_NoFinaliza()
    {
        var sesion = SesionActiva();
        // 1 misión con 2 etapas, solo 1 completada → no debe finalizar
        var ctx = new Contexto(sesion, totalEtapasPorMision: 2, etapasCompletadas: 1);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.SesionActualizada.Should().NotBeNull();
        ctx.SesionActualizada!.Estado.Should().Be(EstadoSesion.Activa);
        ctx.SesionActualizada.EjecucionActual!.EtapaId.Should().NotBe(EtapaId);
    }

    [Fact]
    public async Task SinEtapasEnMision_NoFinaliza()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 0, etapasCompletadas: 0);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SiempreRegistraEtapaCompletada_InclusivoAntesDeVerificar()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 2, etapasCompletadas: 1);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoEtapas.Verify(r => r.RegistrarAsync(
            sesion.Id, EtapaId, AhoraUtc, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MisionRetornaNula_TrataCeroEtapas_NoFinaliza()
    {
        var sesion = SesionActiva();
        var repoSesiones = new Mock<IRepositorioSesiones>();
        repoSesiones.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        var repoEtapas = new Mock<IRepositorioEtapasCompletadas>();
        repoEtapas.Setup(r => r.RegistrarAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        repoEtapas.Setup(r => r.ContarAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var clienteMisiones = new Mock<IClienteJuegosMisiones>();
        clienteMisiones.Setup(c => c.ObtenerMisionConEtapasAsync(
                MisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((MisionConEtapasJuegosDto?)null); // misión no encontrada
        var reloj = new Mock<IProveedorFechaHora>();
        reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(AhoraUtc);
        var unidadTrabajo = new Mock<IUnidadTrabajoSesiones>();
        unidadTrabajo.Setup(u => u.EjecutarEnTransaccionAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((op, ct) => op(ct));
        var servicio = new ServicioFinalizacionSesion(
            repoSesiones.Object, repoEtapas.Object, clienteMisiones.Object,
            Mock.Of<INotificadorSesionesTiempoReal>(),
            unidadTrabajo.Object, reloj.Object);

        await servicio.FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        repoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ======================================================================
    // CIERRE POR VENCIMIENTO DE ETAPA (HU37/HU49 - proceso autónomo)
    // ======================================================================

    [Fact] // La etapa venció y era la última → finaliza la sesión automáticamente.
    public async Task Vencimiento_EtapaVencida_FinalizaUltimaEtapa()
    {
        var sesion = SesionActiva(); // etapa de 60 s desde AhoraUtc
        var ctx = new Contexto(sesion, totalEtapasPorMision: 1, etapasCompletadas: 1,
            ahora: AhoraUtc.AddSeconds(61)); // ya vencida

        await ctx.Construir().AvanzarEtapaPorVencimientoAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.SesionActualizada.Should().NotBeNull();
        ctx.SesionActualizada!.Estado.Should().Be(EstadoSesion.Finalizada);
    }

    [Fact] // La etapa venció y hay siguiente → avanza a la siguiente etapa.
    public async Task Vencimiento_EtapaVencida_AvanzaALaSiguienteEtapa()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 2, etapasCompletadas: 1,
            ahora: AhoraUtc.AddSeconds(61));

        await ctx.Construir().AvanzarEtapaPorVencimientoAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.SesionActualizada.Should().NotBeNull();
        ctx.SesionActualizada!.Estado.Should().Be(EstadoSesion.Activa);
        ctx.SesionActualizada.EjecucionActual!.EtapaId.Should().NotBe(EtapaId);
    }

    [Fact] // Idempotencia/concurrencia (#12): si aún no venció, es un no-op.
    public async Task Vencimiento_EtapaNoVencida_EsNoOp()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 1, etapasCompletadas: 1,
            ahora: AhoraUtc.AddSeconds(10)); // 10 s de 60 → no vencida

        await ctx.Construir().AvanzarEtapaPorVencimientoAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.RepoEtapas.Verify(r => r.RegistrarAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // Concurrencia (#12): si la última respuesta ya avanzó a otra etapa, no reavanza.
    public async Task Vencimiento_EtapaYaAvanzada_NoTransicionaDeNuevo()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 2, etapasCompletadas: 1,
            ahora: AhoraUtc.AddSeconds(61));
        var otraEtapa = Guid.NewGuid(); // la etapa que se intenta cerrar ya no es la actual

        await ctx.Construir().AvanzarEtapaPorVencimientoAsync(
            sesion.Id, otraEtapa, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // #13 pausa: una sesión Pausada nunca cierra su etapa por tiempo.
    public async Task Vencimiento_SesionPausada_NoAvanza()
    {
        var sesion = SesionActiva();
        sesion.Pausar(AhoraUtc.AddSeconds(10)); // pasa a Pausada (ya no es Activa)
        var ctx = new Contexto(sesion, totalEtapasPorMision: 1, etapasCompletadas: 1,
            ahora: AhoraUtc.AddSeconds(300));

        await ctx.Construir().AvanzarEtapaPorVencimientoAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // #13 pausa/reanudación: el tiempo activo descuenta la pausa; la etapa
           // NO vence aunque el reloj de pared pase el fin nominal de 60 s.
    public async Task Vencimiento_EtapaReanudadaTrasPausaLarga_NoVenceAunPasadoElFinNominal()
    {
        var sesion = SesionActiva();                // etapa de 60 s desde AhoraUtc
        sesion.Pausar(AhoraUtc.AddSeconds(10));      // 10 s de juego activo, luego pausa
        sesion.Reanudar(AhoraUtc.AddSeconds(310));   // 5 min pausada; vuelve a Activa
        // ahora = t+320: activo = 10 (antes) + 10 (después) = 20 s → restan 40 s.
        var ctx = new Contexto(sesion, totalEtapasPorMision: 1, etapasCompletadas: 1,
            ahora: AhoraUtc.AddSeconds(320));

        await ctx.Construir().AvanzarEtapaPorVencimientoAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ======================================================================
    // PREPARACIÓN ENTRE ETAPAS / MISIONES (#5/#31)
    // ======================================================================

    [Fact] // Etapa intermedia → PROGRAMA la siguiente en Preparacion; EtapaCompletada
           // + EtapaPorComenzar, pero NO EtapaIniciada todavía.
    public async Task EtapaIntermedia_ProgramaSiguienteEnPreparacion_SinEtapaIniciada()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 2, etapasCompletadas: 1);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.SesionActualizada.Should().NotBeNull();
        ctx.SesionActualizada!.Estado.Should().Be(EstadoSesion.Activa);
        ctx.SesionActualizada.EjecucionActual.Should().NotBeNull();
        ctx.SesionActualizada.EjecucionActual!.EstaEnPreparacion.Should().BeTrue();
        ctx.SesionActualizada.EjecucionActual.EtapaId.Should().NotBe(EtapaId);
        ctx.SesionActualizada.EjecucionActual.DuracionPreparacionSegundos.Should().Be(10);
        ctx.SesionActualizada.EjecucionActual.FechaInicioProgramadaUtc
            .Should().Be(AhoraUtc.AddSeconds(10));

        ctx.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            sesion.Id, MisionId, EtapaId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEtapaPorComenzarAsync(
            sesion.Id, It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<DateTime>(), 10, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEtapaIniciadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // Última etapa → finaliza; NO se programa preparación ni EtapaPorComenzar.
    public async Task UltimaEtapa_NoEmiteEtapaPorComenzar()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion, totalEtapasPorMision: 1, etapasCompletadas: 1);

        await ctx.Construir().FinalizarSiTodasEtapasCompletadasAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.SesionActualizada!.Estado.Should().Be(EstadoSesion.Finalizada);
        ctx.Notificador.Verify(n => n.NotificarEtapaPorComenzarAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // #13 activación server-side: activa la etapa preparada y emite EtapaIniciada una vez.
    public async Task ActivarEtapaProgramada_ActivaLaEtapaYEmiteEtapaIniciada()
    {
        var sesion = SesionActiva();
        var siguienteEtapaId = Guid.NewGuid();
        sesion.ProgramarSiguienteEtapa(
            EtapaId,
            EjecucionActualSesion.Planificar(
                MisionId, siguienteEtapaId, Guid.NewGuid(), "Trivia", 2, 1, 2, 60),
            AhoraUtc,
            10);
        var ctx = new Contexto(sesion, ahora: AhoraUtc.AddSeconds(11)); // preparación vencida

        await ctx.Construir().ActivarEtapaProgramadaAsync(
            sesion.Id, siguienteEtapaId, CancellationToken.None);

        ctx.SesionActualizada!.EjecucionActual!.EstaActiva.Should().BeTrue();
        ctx.Notificador.Verify(n => n.NotificarEtapaIniciadaAsync(
            sesion.Id, MisionId, siguienteEtapaId, It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // #13: antes de vencer la preparación es un no-op (sin EtapaIniciada).
    public async Task ActivarEtapaProgramada_AntesDeVencer_EsNoOp()
    {
        var sesion = SesionActiva();
        var siguienteEtapaId = Guid.NewGuid();
        sesion.ProgramarSiguienteEtapa(
            EtapaId,
            EjecucionActualSesion.Planificar(
                MisionId, siguienteEtapaId, Guid.NewGuid(), "Trivia", 2, 1, 2, 60),
            AhoraUtc,
            10);
        var ctx = new Contexto(sesion, ahora: AhoraUtc.AddSeconds(5)); // 5 s < 10 s

        await ctx.Construir().ActivarEtapaProgramadaAsync(
            sesion.Id, siguienteEtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarEtapaIniciadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    // ======================================================================
    // CIERRE PENDIENTE (feedback final antes de cerrar, #13/#15)
    // ======================================================================

    [Fact] // Todos completaron → CierrePendiente, sin emitir EtapaCompletada/PorComenzar.
    public async Task ProgramarCierreTrasFeedback_EntraEnCierrePendiente_SinEventos()
    {
        var sesion = SesionActiva();
        var ctx = new Contexto(sesion);

        await ctx.Construir().ProgramarCierreTrasFeedbackAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.SesionActualizada.Should().NotBeNull();
        ctx.SesionActualizada!.EjecucionActual!.EstaEnCierrePendiente.Should().BeTrue();
        ctx.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarEtapaPorComenzarAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // Feedback final vencido → cierra realmente + programa siguiente + eventos.
    public async Task CerrarTrasCierrePendiente_Vencido_CierraYProgramaSiguiente()
    {
        var sesion = SesionActiva();
        sesion.ProgramarCierrePendiente(EtapaId, AhoraUtc, 5);
        var ctx = new Contexto(sesion, totalEtapasPorMision: 2, etapasCompletadas: 1,
            ahora: AhoraUtc.AddSeconds(6)); // feedback vencido

        await ctx.Construir().CerrarEtapaTrasCierrePendienteAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.SesionActualizada!.EjecucionActual!.EstaEnPreparacion.Should().BeTrue();
        ctx.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            sesion.Id, MisionId, EtapaId, It.IsAny<CancellationToken>()), Times.Once);
        ctx.Notificador.Verify(n => n.NotificarEtapaPorComenzarAsync(
            sesion.Id, It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(),
            It.IsAny<DateTime>(), 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // #23: antes de vencer el feedback es no-op (pausa/idempotencia).
    public async Task CerrarTrasCierrePendiente_AntesDeVencer_EsNoOp()
    {
        var sesion = SesionActiva();
        sesion.ProgramarCierrePendiente(EtapaId, AhoraUtc, 5);
        var ctx = new Contexto(sesion, totalEtapasPorMision: 2, etapasCompletadas: 1,
            ahora: AhoraUtc.AddSeconds(3)); // 3 s < 5 s

        await ctx.Construir().CerrarEtapaTrasCierrePendienteAsync(
            sesion.Id, EtapaId, CancellationToken.None);

        ctx.RepoSesiones.Verify(r => r.ActualizarAsync(
            It.IsAny<Sesion>(), It.IsAny<CancellationToken>()), Times.Never);
        ctx.Notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<Guid>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
