using System;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.PruebasUnitarias.Dominio;

public class EjecucionActualSesionInvariantesPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 16, 14, 0, 0, DateTimeKind.Utc);
    private static readonly Guid MisionId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid EtapaId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ModoId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Theory]
    [InlineData(0, 1, "orden de misión")]
    [InlineData(1, 0, "orden de etapa")]
    public void Planificar_ConOrdenNoPositivo_Lanza(int ordenMision, int ordenEtapa, string mensaje)
    {
        Action accion = () => EjecucionActualSesion.Planificar(
            MisionId, EtapaId, ModoId, "Trivia", 1, ordenMision, ordenEtapa, 60);

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage($"*{mensaje}*");
    }

    [Fact]
    public void Rehidratar_ConPausasNegativas_Lanza()
    {
        Action accion = () => EjecucionActualSesion.Rehidratar(
            MisionId, EtapaId, ModoId, "Trivia", 1, AhoraUtc, 60, -1, null);

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*pausas no puede ser negativa*");
    }

    [Fact]
    public void Rehidratar_PlanificadaConFechaInicio_Lanza()
    {
        Action accion = () => EjecucionActualSesion.Rehidratar(
            MisionId,
            EtapaId,
            ModoId,
            "Trivia",
            1,
            AhoraUtc,
            60,
            0,
            null,
            FaseEjecucionEtapaSesion.Planificada);

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*planificada no puede tener fecha*");
    }

    [Fact]
    public void Rehidratar_PlanificadaConPausa_Lanza()
    {
        Action accion = () => EjecucionActualSesion.Rehidratar(
            MisionId,
            EtapaId,
            ModoId,
            "Trivia",
            1,
            null,
            60,
            100,
            AhoraUtc,
            FaseEjecucionEtapaSesion.Planificada);

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*planificada no tiene preparación ni pausas*");
    }

    [Theory]
    [InlineData(FaseEjecucionEtapaSesion.Preparacion)]
    [InlineData(FaseEjecucionEtapaSesion.Activa)]
    [InlineData(FaseEjecucionEtapaSesion.CierrePendiente)]
    public void Rehidratar_FaseRuntimeSinFechaInicio_Lanza(FaseEjecucionEtapaSesion fase)
    {
        Action accion = () => EjecucionActualSesion.Rehidratar(
            MisionId, EtapaId, ModoId, "Trivia", 1, null, 60, 0, null, fase);

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*requiere fecha de inicio*");
    }

    [Theory]
    [InlineData(FaseEjecucionEtapaSesion.Preparacion)]
    [InlineData(FaseEjecucionEtapaSesion.CierrePendiente)]
    public void Rehidratar_PreparacionOCierreSinDuracion_Lanza(FaseEjecucionEtapaSesion fase)
    {
        Action accion = () => EjecucionActualSesion.Rehidratar(
            MisionId,
            EtapaId,
            ModoId,
            "Trivia",
            1,
            AhoraUtc,
            60,
            0,
            null,
            fase,
            duracionPreparacionSegundos: 0);

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*requieren una duración mayor a cero*");
    }

    [Fact]
    public void FechaInicioProgramada_Planificada_Lanza()
    {
        var etapa = EjecucionActualSesion.Planificar(
            MisionId, EtapaId, ModoId, "Trivia", 1, 1, 1, 60);

        Action accion = () => _ = etapa.FechaInicioProgramadaUtc;

        accion.Should().Throw<SesionInvalidaExcepcion>()
            .WithMessage("*no tiene fecha de inicio programada*");
    }

    [Fact]
    public void Crear_NormalizaFechaLocalComoUtc()
    {
        var fechaLocal = new DateTime(2026, 7, 16, 9, 0, 0, DateTimeKind.Local);

        var etapa = EjecucionActualSesion.Crear(
            MisionId, EtapaId, ModoId, " Trivia ", 1, fechaLocal, 60);

        etapa.FechaInicioUtc.Should().Be(DateTime.SpecifyKind(fechaLocal, DateTimeKind.Utc));
        etapa.FechaInicioUtc!.Value.Kind.Should().Be(DateTimeKind.Utc);
        etapa.TipoEtapa.Should().Be("Trivia");
    }

    [Fact]
    public void CalcularSegundosRestantes_NoDevuelveValoresNegativos()
    {
        var etapa = EjecucionActualSesion.Crear(
            MisionId, EtapaId, ModoId, "Trivia", 1, AhoraUtc, 10);

        etapa.CalcularSegundosRestantes(AhoraUtc.AddMinutes(5)).Should().Be(0);
        etapa.CalcularTiempoActivoTranscurridoMs(AhoraUtc.AddSeconds(-5)).Should().Be(0);
    }

    [Fact]
    public void Equals_YGetHashCode_ComparanTodosLosCampos()
    {
        var etapa = EjecucionActualSesion.Rehidratar(
            MisionId, EtapaId, ModoId, "Trivia", 1, AhoraUtc, 60, 10, null);
        var igual = EjecucionActualSesion.Rehidratar(
            MisionId, EtapaId, ModoId, "Trivia", 1, AhoraUtc, 60, 10, null);
        var diferente = EjecucionActualSesion.Rehidratar(
            MisionId, Guid.NewGuid(), ModoId, "Trivia", 1, AhoraUtc, 60, 10, null);

        etapa.Equals(igual).Should().BeTrue();
        etapa.Equals((object)igual).Should().BeTrue();
        etapa.GetHashCode().Should().Be(igual.GetHashCode());
        EjecucionActualSesion? nula = null;

        etapa.Equals(diferente).Should().BeFalse();
        etapa.Equals(nula).Should().BeFalse();
        etapa.Equals("otra cosa").Should().BeFalse();
    }
}
