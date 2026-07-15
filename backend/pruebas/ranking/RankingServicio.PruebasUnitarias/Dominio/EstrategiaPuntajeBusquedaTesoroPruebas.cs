using FluentAssertions;
using RankingServicio.Dominio.Estrategias;
using RankingServicio.Dominio.Excepciones;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Dominio;

// Reduccion automatica del puntaje de Busqueda del Tesoro segun orden de
// resolucion y cantidad de competidores. El tiempo se conserva en el contrato,
// pero ya no participa en la formula.
public sealed class EstrategiaPuntajeBusquedaTesoroPruebas
{
    private readonly EstrategiaPuntajeBusquedaTesoro _estrategia = new();

    private const int TiempoLimiteMs = 300_000;

    [Fact]
    public void EvidenciaInvalida_devuelveCero()
        => Calcular(false, 30, 1, 3).Should().Be(0);

    [Fact]
    public void UnicoCompetidor_devuelvePuntajeBase()
        => Calcular(true, 30, 1, 1).Should().Be(30);

    [Fact]
    public void DosCompetidores_primeroCompletoSegundoMitad()
        => CalcularSecuencia(30, 2).Should().Equal(30, 15);

    [Fact]
    public void TresCompetidores_base30_aplicaPenalizacionDe10()
        => CalcularSecuencia(30, 3).Should().Equal(30, 20, 10);

    [Fact]
    public void CincoCompetidores_base30_aplicaPenalizacionDe6()
        => CalcularSecuencia(30, 5).Should().Equal(30, 24, 18, 12, 6);

    [Fact]
    public void SieteCompetidores_base30_redondeaPenalizacionA4()
        => CalcularSecuencia(30, 7).Should().Equal(30, 26, 22, 18, 14, 10, 6);

    [Fact]
    public void DivisionMenorAmedio_penalizacionNuncaEsCero_yPuntajeValidoNuncaEsCero()
        => CalcularSecuencia(3, 10).Should().Equal(3, 2, 1, 1, 1, 1, 1, 1, 1, 1);

    [Fact]
    public void BaseUno_evidenciaValidaSiempreRecibeMinimoUno()
        => CalcularSecuencia(1, 5).Should().Equal(1, 1, 1, 1, 1);

    [Fact]
    public void PrimerCompetidor_siempreRecibePuntajeBase()
    {
        Calcular(true, 30, 1, 3, tiempoTranscurridoMs: 0).Should().Be(30);
        Calcular(true, 30, 1, 3, tiempoTranscurridoMs: 299_000).Should().Be(30);
    }

    [Fact]
    public void UltimoCompetidor_recibeFormula_yNoMinimoFijoDel20PorCiento()
    {
        Calcular(true, 30, 2, 2).Should().Be(15);
        Calcular(true, 30, 3, 3).Should().Be(10);
        Calcular(true, 30, 7, 7).Should().Be(6);
    }

    [Fact]
    public void RedondeoDePenalizacion_usaAwayFromZero()
        => Calcular(true, 5, 2, 2).Should().Be(2);

    [Fact]
    public void TiempoTranscurridoYLimite_noAfectanPuntaje()
    {
        var alInicio = Calcular(true, 30, 2, 3, tiempoTranscurridoMs: 0, tiempoLimiteMs: TiempoLimiteMs);
        var alFinal = Calcular(true, 30, 2, 3, tiempoTranscurridoMs: TiempoLimiteMs, tiempoLimiteMs: TiempoLimiteMs);
        var sinLimite = Calcular(true, 30, 2, 3, tiempoTranscurridoMs: 5_000, tiempoLimiteMs: 0);

        alInicio.Should().Be(20);
        alFinal.Should().Be(alInicio);
        sinLimite.Should().Be(alInicio);
    }

    [Fact]
    public void CasoIndividual_tresParticipantes_usaParticipantesComoCompetidores()
        => CalcularSecuencia(30, 3).Should().Equal(30, 20, 10);

    [Fact]
    public void CasoGrupal_dosEquipos_usaEquiposComoCompetidores()
        => CalcularSecuencia(30, 2).Should().Equal(30, 15);

    [Fact]
    public void PuntajeBaseNoPositivo_conEvidenciaValida_devuelveMinimoUno()
    {
        Calcular(true, 0, 1, 3).Should().Be(1);
        Calcular(true, -5, 2, 3).Should().Be(1);
    }

    [Fact]
    public void TotalCompetidoresInvalido_lanzaExcepcion()
    {
        var accion = () => Calcular(true, 30, 1, 0);

        accion.Should().Throw<RankingInvalidoExcepcion>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    public void OrdenResolucionInvalido_lanzaExcepcion(int orden)
    {
        var accion = () => Calcular(true, 30, orden, 3);

        accion.Should().Throw<RankingInvalidoExcepcion>();
    }

    private int Calcular(
        bool esValida,
        int puntajeBase,
        int ordenResolucion,
        int totalCompetidores,
        int tiempoTranscurridoMs = 0,
        int tiempoLimiteMs = TiempoLimiteMs)
        => (int)_estrategia.Calcular(new(
            esValida,
            puntajeBase,
            ordenResolucion,
            totalCompetidores,
            tiempoTranscurridoMs,
            tiempoLimiteMs)).Valor;

    private int[] CalcularSecuencia(int puntajeBase, int totalCompetidores)
        => Enumerable.Range(1, totalCompetidores)
            .Select(orden => Calcular(true, puntajeBase, orden, totalCompetidores))
            .ToArray();
}
