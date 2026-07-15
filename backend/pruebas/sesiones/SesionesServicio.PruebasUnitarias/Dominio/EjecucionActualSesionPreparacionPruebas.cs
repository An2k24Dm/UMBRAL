using System;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

// Fase de Preparacion/Activa de EjecucionActualSesion (espera de 10 s entre
// etapas/misiones). El Value Object sigue siendo inmutable.
public class EjecucionActualSesionPreparacionPruebas
{
    private static readonly DateTime Ahora = new(2026, 7, 10, 12, 0, 0, DateTimeKind.Utc);
    private static readonly Guid M = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid E = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Mo = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static EjecucionActualSesion Planificada(
        int ordenGlobal = 2, int ordenMision = 1, int ordenEtapa = 2, int dur = 60)
        => EjecucionActualSesion.Planificar(M, E, Mo, "Trivia", ordenGlobal, ordenMision, ordenEtapa, dur);

    private static EjecucionActualSesion Programada(int prep = 10)
        => Planificada().Programar(Ahora, prep);

    [Fact] // #30.1
    public void Programar_QuedaEnPreparacion_ConFechaProgramadaFuturaYRelojEnCero()
    {
        var e = Programada();

        e.EstaEnPreparacion.Should().BeTrue();
        e.EstaActiva.Should().BeFalse();
        e.Fase.Should().Be(FaseEjecucionEtapaSesion.Preparacion);
        e.FechaInicioProgramadaUtc.Should().Be(Ahora.AddSeconds(10));
        e.CalcularSegundosRestantesPreparacion(Ahora).Should().Be(10);
        e.PreparacionVencida(Ahora).Should().BeFalse();
        // Durante la preparación el reloj de juego de la etapa está congelado en 0.
        e.CalcularTiempoActivoTranscurridoMs(Ahora).Should().Be(0);
        e.OrdenMision.Should().Be(1);
        e.OrdenEtapa.Should().Be(2);
        e.EsNuevaMision.Should().BeFalse();
    }

    [Fact] // #30.4
    public void NoVenceAntesDeLaFecha()
    {
        var e = Programada();

        e.PreparacionVencida(Ahora.AddSeconds(9)).Should().BeFalse();
        e.CalcularSegundosRestantesPreparacion(Ahora.AddSeconds(9)).Should().Be(1);
    }

    [Fact] // #30.3
    public void Activar_AlLlegarLaFecha_PasaAActivaConRelojDeJuegoCompleto()
    {
        Programada().PreparacionVencida(Ahora.AddSeconds(10)).Should().BeTrue();

        var activa = Programada().Activar(Ahora.AddSeconds(10));

        activa.EstaActiva.Should().BeTrue();
        activa.Fase.Should().Be(FaseEjecucionEtapaSesion.Activa);
        activa.CalcularSegundosRestantes(Ahora.AddSeconds(10)).Should().Be(60);
        activa.CalcularSegundosRestantes(Ahora.AddSeconds(20)).Should().Be(50);
    }

    [Fact] // #30.5
    public void Activar_EsIdempotente_SiYaEstabaActiva()
    {
        var activa = Programada().Activar(Ahora.AddSeconds(10));

        var otra = activa.Activar(Ahora.AddSeconds(15));

        otra.Should().BeSameAs(activa);
    }

    [Fact] // #30.6/#30.7
    public void PausaDurantePreparacion_CongelaCountdown_ReanudarConservaTiempoRestante()
    {
        // Transcurren 3 s (quedan 7). Pausa en t=3, reanuda 2 min después (t=123).
        var e = Programada()
            .Pausar(Ahora.AddSeconds(3))
            .Reanudar(Ahora.AddSeconds(123));

        e.CalcularSegundosRestantesPreparacion(Ahora.AddSeconds(123)).Should().Be(7);
        e.PreparacionVencida(Ahora.AddSeconds(123)).Should().BeFalse();
    }

    [Fact]
    public void EsNuevaMision_CuandoLaEtapaProgramadaEsLaPrimeraDeSuMision()
    {
        var e = EjecucionActualSesion.Planificar(M, E, Mo, "Trivia", 3, 2, 1, 60).Programar(Ahora, 10);

        e.EsNuevaMision.Should().BeTrue();
        e.OrdenMision.Should().Be(2);
        e.OrdenEtapa.Should().Be(1);
    }

    // --- Cierre pendiente (feedback final antes de cerrar la etapa) ---

    private static EjecucionActualSesion Activa()
        => EjecucionActualSesion.Crear(M, E, Mo, "Trivia", 1, Ahora, 60);

    [Fact]
    public void ProgramarCierrePendiente_QuedaEnCierrePendiente_ConCountdownDeCincoSegundos()
    {
        var e = Activa().ProgramarCierrePendiente(Ahora, 5);

        e.EstaEnCierrePendiente.Should().BeTrue();
        e.EstaActiva.Should().BeFalse();
        e.Fase.Should().Be(FaseEjecucionEtapaSesion.CierrePendiente);
        e.CalcularSegundosRestantesCierrePendiente(Ahora).Should().Be(5);
        e.CierrePendienteVencido(Ahora).Should().BeFalse();
        e.CierrePendienteVencido(Ahora.AddSeconds(4)).Should().BeFalse();
        e.CierrePendienteVencido(Ahora.AddSeconds(5)).Should().BeTrue();
    }

    [Fact]
    public void PausaDuranteCierrePendiente_CongelaElCountdown_ReanudarConserva()
    {
        // Transcurren 2 s (quedan 3). Pausa en t=2, reanuda 2 min después.
        var e = Activa()
            .ProgramarCierrePendiente(Ahora, 5)
            .Pausar(Ahora.AddSeconds(2))
            .Reanudar(Ahora.AddSeconds(122));

        e.CalcularSegundosRestantesCierrePendiente(Ahora.AddSeconds(122)).Should().Be(3);
        e.CierrePendienteVencido(Ahora.AddSeconds(122)).Should().BeFalse();
    }

    // --- Fase Planificada: única representación canónica del PLAN (#19) ---

    [Fact]
    public void Planificar_CreaEtapaPlanificada_SinFechaDeInicio()
    {
        var e = EjecucionActualSesion.Planificar(M, E, Mo, "Trivia", 1, 1, 1, 60);

        e.EstaPlanificada.Should().BeTrue();
        e.Fase.Should().Be(FaseEjecucionEtapaSesion.Planificada);
        e.FechaInicioUtc.Should().BeNull();
        e.DuracionPreparacionSegundos.Should().Be(0);
        e.DuracionPausasAcumuladaMs.Should().Be(0);
        e.CalcularTiempoActivoTranscurridoMs(Ahora).Should().Be(0);
    }

    [Fact]
    public void PlanificadaIniciar_PasaAActiva_ConFechaDeInicio()
    {
        var activa = EjecucionActualSesion.Planificar(M, E, Mo, "Trivia", 1, 1, 1, 60)
            .Iniciar(Ahora);

        activa.EstaActiva.Should().BeTrue();
        activa.Fase.Should().Be(FaseEjecucionEtapaSesion.Activa);
        activa.FechaInicioUtc.Should().Be(Ahora);
    }

    [Fact]
    public void PlanificadaProgramar_PasaAPreparacion()
    {
        var prep = EjecucionActualSesion.Planificar(M, E, Mo, "Trivia", 1, 1, 1, 60)
            .Programar(Ahora, 10);

        prep.EstaEnPreparacion.Should().BeTrue();
        prep.Fase.Should().Be(FaseEjecucionEtapaSesion.Preparacion);
        prep.FechaInicioUtc.Should().Be(Ahora);
        prep.DuracionPreparacionSegundos.Should().Be(10);
    }

    [Fact]
    public void FlujoCanonico_Planificada_Preparacion_Activa_CierrePendiente()
    {
        var planificada = EjecucionActualSesion.Planificar(M, E, Mo, "Trivia", 1, 1, 1, 60);
        var preparacion = planificada.Programar(Ahora, 10);
        var activa = preparacion.Activar(Ahora.AddSeconds(10));
        var cierre = activa.ProgramarCierrePendiente(Ahora.AddSeconds(20), 5);

        planificada.EstaPlanificada.Should().BeTrue();
        preparacion.EstaEnPreparacion.Should().BeTrue();
        activa.EstaActiva.Should().BeTrue();
        cierre.EstaEnCierrePendiente.Should().BeTrue();
    }

    [Fact] // #19.7 — transiciones imposibles
    public void TransicionesImposibles_LanzanExcepcion()
    {
        var planificada = EjecucionActualSesion.Planificar(M, E, Mo, "Trivia", 1, 1, 1, 60);
        var activa = planificada.Iniciar(Ahora);
        var cierre = activa.ProgramarCierrePendiente(Ahora, 5);

        // Planificada no puede ir directo a CierrePendiente ni activarse.
        planificada.Invoking(x => x.ProgramarCierrePendiente(Ahora, 5)).Should().Throw<Exception>();
        planificada.Invoking(x => x.Activar(Ahora)).Should().Throw<Exception>();
        // Una etapa activa no puede "planificarse" de nuevo ni programarse.
        activa.Invoking(x => x.Iniciar(Ahora)).Should().Throw<Exception>();
        activa.Invoking(x => x.Programar(Ahora, 10)).Should().Throw<Exception>();
        // CierrePendiente no puede activarse directamente.
        cierre.Invoking(x => x.Activar(Ahora)).Should().Throw<Exception>();
    }
}
