using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using SesionesServicio.Aplicacion.Procesos.VencimientoEtapas;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Servicios;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.ObjetosValor;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.Persistencia.Mapeadores;
using SesionesServicio.Infraestructura.Persistencia.Repositorios;

namespace SesionesServicio.PruebasIntegracion;

// Prueba de PERSISTENCIA REAL del avance secuencial de etapas (regresión f981cc6).
// Usa un ContextoSesiones InMemory con la MISMA raíz entre "scopes", el mapeador
// y el repositorio reales, y un NUEVO DbContext por paso para demostrar que el
// estado (Fase, EtapaId, SecuenciaEtapas) se persiste y se rehidrata de verdad.
//
// Clave de la corrección: el cliente HTTP hacia juegos-servicio está configurado
// para LANZAR excepción. Si el worker intentara reconstruir la secuencia por HTTP
// (como en f981cc6, que fallaba con 401 sin token), la transición no avanzaría y
// la prueba fallaría. Como la secuencia se persiste al iniciar, el worker avanza
// leyéndola del agregado, sin tocar juegos-servicio.
public sealed class AvanceSecuencialEtapasPersistenciaPruebas
{
    private static readonly Guid OperadorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ParticipanteId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Guid Mision1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Mision2 = Guid.Parse("22222222-1111-1111-1111-111111111111");
    private static readonly Guid Etapa1 = Guid.Parse("aaaa1111-1111-1111-1111-111111111111");
    private static readonly Guid Etapa2 = Guid.Parse("bbbb2222-2222-2222-2222-222222222222");
    private static readonly Guid ModoTrivia = Guid.Parse("cccc3333-3333-3333-3333-333333333333");
    private static readonly Guid ModoTesoro = Guid.Parse("dddd4444-4444-4444-4444-444444444444");

    private readonly InMemoryDatabaseRoot _raiz = new();
    private readonly RelojControlable _reloj = new(new DateTime(2026, 07, 11, 10, 0, 0, DateTimeKind.Utc));
    private readonly RepositorioEtapasCompletadasFake _etapasCompletadas = new();
    private readonly Mock<INotificadorSesionesTiempoReal> _notificador = new();
    private readonly Mock<IClienteJuegosMisiones> _juegos = new();

    public AvanceSecuencialEtapasPersistenciaPruebas()
    {
        // El worker NUNCA debe llamar a juegos-servicio para avanzar de etapa.
        _juegos
            .Setup(c => c.ObtenerMisionConEtapasAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(
                "El avance en segundo plano NO debe llamar a juegos-servicio (no hay token)."));
    }

    // PASO 1 y 2 del flujo individual (Misión 1 Trivia → Misión 2 Tesoro). El plan
    // se expresa con el Value Object canónico en fase Planificada.
    private IReadOnlyList<EjecucionActualSesion> SecuenciaDosEtapas() => new[]
    {
        EjecucionActualSesion.Planificar(Mision1, Etapa1, ModoTrivia, "Trivia", 1, 1, 1, 120),
        EjecucionActualSesion.Planificar(Mision2, Etapa2, ModoTesoro, "BusquedaTesoro", 2, 2, 1, 120)
    };

    [Fact]
    public async Task Individual_FlujoCompleto_Activa_CierrePendiente_Preparacion_Activa()
    {
        // PASO 1-2: iniciar sesión individual con dos etapas (Trivia, Tesoro).
        var sesion = SesionIndividual.Crear(
            "Sesión de prueba", "Descripción", _reloj.ObtenerFechaHoraUtc().AddMinutes(-5),
            "COD001", OperadorId, _reloj.ObtenerFechaHoraUtc(), maximoParticipantes: 5);
        sesion.AsignarMisiones(new[] { Mision1, Mision2 });
        sesion.Preparar();
        sesion.AgregarParticipante(ParticipanteId, _reloj.ObtenerFechaHoraUtc());
        sesion.EstablecerSecuenciaEtapas(SecuenciaDosEtapas());
        sesion.IniciarPrimeraEtapa(Mision1, Etapa1, ModoTrivia, "Trivia", 1,
            _reloj.ObtenerFechaHoraUtc(), 120);

        await PersistirNuevaAsync(sesion);

        // PASO 2 (assert, nuevo scope): Etapa1 Activa y secuencia persistida.
        await using (var ctx = NuevoContexto())
        {
            var recuperada = await RepositorioDe(ctx).ObtenerPorIdAsync(sesion.Id, default);
            recuperada!.EjecucionActual!.EtapaId.Should().Be(Etapa1);
            recuperada.EjecucionActual.Fase.Should().Be(FaseEjecucionEtapaSesion.Activa);
            recuperada.SecuenciaEtapas.Should().HaveCount(2);
            recuperada.SecuenciaEtapas[1].EtapaId.Should().Be(Etapa2);
        }

        // PASO 3: todos terminan Etapa1 → CierrePendiente (lo que hace el manejador).
        await EnScopeAsync(async s => await s.Finalizacion.ProgramarCierreTrasFeedbackAsync(
            sesion.Id, Etapa1, default));

        // PASO 4 (nuevo scope): el CierrePendiente quedó persistido de verdad.
        await using (var ctx = NuevoContexto())
        {
            var recuperada = await RepositorioDe(ctx).ObtenerPorIdAsync(sesion.Id, default);
            recuperada!.EjecucionActual!.Fase.Should().Be(FaseEjecucionEtapaSesion.CierrePendiente);
            recuperada.EjecucionActual.EtapaId.Should().Be(Etapa1);
            recuperada.EjecucionActual.CierrePendienteVencido(
                _reloj.ObtenerFechaHoraUtc().AddSeconds(6)).Should().BeTrue();
        }

        // PASO 5: pasan > 5 s y corre el worker → cierra Etapa1 y programa Etapa2 (Preparacion).
        _reloj.Avanzar(TimeSpan.FromSeconds(6));
        await EnScopeAsync(async s => await s.Procesador.EjecutarCicloAsync(default));

        await using (var ctx = NuevoContexto())
        {
            var recuperada = await RepositorioDe(ctx).ObtenerPorIdAsync(sesion.Id, default);
            recuperada!.EjecucionActual!.EtapaId.Should().Be(Etapa2);
            recuperada.EjecucionActual.Fase.Should().Be(FaseEjecucionEtapaSesion.Preparacion);
            recuperada.EjecucionActual.MisionId.Should().Be(Mision2);
        }
        _etapasCompletadas.Contiene(sesion.Id, Etapa1).Should().BeTrue();

        // PASO 6: pasan > 10 s y corre el worker → activa Etapa2 (Activa + EtapaIniciada).
        _reloj.Avanzar(TimeSpan.FromSeconds(11));
        await EnScopeAsync(async s => await s.Procesador.EjecutarCicloAsync(default));

        await using (var ctx = NuevoContexto())
        {
            var recuperada = await RepositorioDe(ctx).ObtenerPorIdAsync(sesion.Id, default);
            recuperada!.EjecucionActual!.EtapaId.Should().Be(Etapa2);
            recuperada.EjecucionActual.Fase.Should().Be(FaseEjecucionEtapaSesion.Activa);
        }

        // Eventos SignalR: EtapaCompletada + EtapaPorComenzar (cierre) y EtapaIniciada (activación).
        _notificador.Verify(n => n.NotificarEtapaCompletadaAsync(
            sesion.Id, Mision1, Etapa1, It.IsAny<CancellationToken>()), Times.Once);
        _notificador.Verify(n => n.NotificarEtapaPorComenzarAsync(
            sesion.Id, Mision2, Etapa2, It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), true,
            It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificador.Verify(n => n.NotificarEtapaIniciadaAsync(
            sesion.Id, Mision2, Etapa2, It.IsAny<string>(), It.IsAny<Guid>(),
            It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<int>(),
            It.IsAny<CancellationToken>()), Times.Once);

        // El worker NUNCA tocó juegos-servicio.
        _juegos.Verify(c => c.ObtenerMisionConEtapasAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Grupal_FlujoCompleto_Activa_CierrePendiente_Preparacion_Activa()
    {
        var sesion = SesionGrupal.Crear(
            "Sesión grupal", "Descripción", _reloj.ObtenerFechaHoraUtc().AddMinutes(-5),
            "COD002", OperadorId, _reloj.ObtenerFechaHoraUtc(),
            maximoEquipos: 3, maximoParticipantesPorEquipo: 4);
        sesion.AsignarMisiones(new[] { Mision1, Mision2 });
        sesion.Preparar();
        sesion.CrearEquipo(NombreEquipo.Crear("Rojo"), TipoEquipo.Publico, null,
            Guid.NewGuid(), _reloj.ObtenerFechaHoraUtc(), _reloj.ObtenerFechaHoraUtc());
        sesion.CrearEquipo(NombreEquipo.Crear("Azul"), TipoEquipo.Publico, null,
            Guid.NewGuid(), _reloj.ObtenerFechaHoraUtc(), _reloj.ObtenerFechaHoraUtc());
        sesion.EstablecerSecuenciaEtapas(SecuenciaDosEtapas());
        sesion.IniciarPrimeraEtapa(Mision1, Etapa1, ModoTrivia, "Trivia", 1,
            _reloj.ObtenerFechaHoraUtc(), 120);

        await PersistirNuevaAsync(sesion);

        // La sesión grupal también persiste y rehidrata su plan de etapas.
        await using (var ctx = NuevoContexto())
        {
            var recuperada = await RepositorioDe(ctx).ObtenerPorIdAsync(sesion.Id, default);
            recuperada.Should().BeOfType<SesionGrupal>();
            recuperada!.SecuenciaEtapas.Should().HaveCount(2);
            recuperada.EjecucionActual!.Fase.Should().Be(FaseEjecucionEtapaSesion.Activa);
        }

        // Cuando TODOS los equipos terminan Etapa1 → CierrePendiente.
        await EnScopeAsync(async s => await s.Finalizacion.ProgramarCierreTrasFeedbackAsync(
            sesion.Id, Etapa1, default));

        _reloj.Avanzar(TimeSpan.FromSeconds(6));
        await EnScopeAsync(async s => await s.Procesador.EjecutarCicloAsync(default));
        await using (var ctx = NuevoContexto())
        {
            var recuperada = await RepositorioDe(ctx).ObtenerPorIdAsync(sesion.Id, default);
            recuperada!.EjecucionActual!.EtapaId.Should().Be(Etapa2);
            recuperada.EjecucionActual.Fase.Should().Be(FaseEjecucionEtapaSesion.Preparacion);
        }

        _reloj.Avanzar(TimeSpan.FromSeconds(11));
        await EnScopeAsync(async s => await s.Procesador.EjecutarCicloAsync(default));
        await using (var ctx = NuevoContexto())
        {
            var recuperada = await RepositorioDe(ctx).ObtenerPorIdAsync(sesion.Id, default);
            recuperada!.EjecucionActual!.EtapaId.Should().Be(Etapa2);
            recuperada.EjecucionActual.Fase.Should().Be(FaseEjecucionEtapaSesion.Activa);
        }

        _juegos.Verify(c => c.ObtenerMisionConEtapasAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- Infraestructura de prueba (persistencia real, nuevo scope por paso) ----

    private ContextoSesiones NuevoContexto()
        => new(new DbContextOptionsBuilder<ContextoSesiones>()
            .UseInMemoryDatabase("avance-secuencial", _raiz)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options);

    private static RepositorioSesiones RepositorioDe(ContextoSesiones ctx)
        => new(ctx, new MapeadorSesionesPersistencia(new IMapeadorPersistenciaSesion[]
        {
            new MapeadorPersistenciaSesionIndividual(),
            new MapeadorPersistenciaSesionGrupal()
        }));

    private async Task PersistirNuevaAsync(Sesion sesion)
    {
        await using var ctx = NuevoContexto();
        await RepositorioDe(ctx).AgregarAsync(sesion, default);
        await ctx.SaveChangesAsync();
    }

    private async Task EnScopeAsync(Func<Servicios, Task> accion)
    {
        await using var ctx = NuevoContexto();
        var repo = RepositorioDe(ctx);
        var unidad = new UnidadTrabajoSesiones(ctx);
        var finalizacion = new ServicioFinalizacionSesion(
            repo, _etapasCompletadas, _juegos.Object, _notificador.Object, unidad, _reloj);
        var procesador = new ProcesadorVencimientoEtapasSesion(
            repo, finalizacion, _reloj, Mock.Of<IRegistroLogsAplicacion>());
        await accion(new Servicios(finalizacion, procesador));
        await ctx.SaveChangesAsync();
    }

    private sealed record Servicios(
        IServicioFinalizacionSesion Finalizacion,
        ProcesadorVencimientoEtapasSesion Procesador);

    private sealed class RelojControlable : IProveedorFechaHora
    {
        private DateTime _ahora;
        public RelojControlable(DateTime inicial) => _ahora = inicial;
        public DateTime ObtenerFechaHoraUtc() => _ahora;
        public void Avanzar(TimeSpan delta) => _ahora = _ahora.Add(delta);
    }

    private sealed class RepositorioEtapasCompletadasFake : IRepositorioEtapasCompletadas
    {
        private readonly HashSet<(Guid Sesion, Guid Etapa)> _registradas = new();

        public Task<bool> RegistrarAsync(Guid sesionId, Guid etapaId, DateTime fechaUtc, CancellationToken cancelacion)
            => Task.FromResult(_registradas.Add((sesionId, etapaId)));

        public Task<int> ContarAsync(Guid sesionId, CancellationToken cancelacion)
            => Task.FromResult(_registradas.Count(r => r.Sesion == sesionId));

        public Task<IReadOnlyList<Guid>> ObtenerCompletadasAsync(Guid sesionId, CancellationToken cancelacion)
            => Task.FromResult((IReadOnlyList<Guid>)_registradas
                .Where(r => r.Sesion == sesionId).Select(r => r.Etapa).ToList());

        public bool Contiene(Guid sesionId, Guid etapaId) => _registradas.Contains((sesionId, etapaId));
    }
}
