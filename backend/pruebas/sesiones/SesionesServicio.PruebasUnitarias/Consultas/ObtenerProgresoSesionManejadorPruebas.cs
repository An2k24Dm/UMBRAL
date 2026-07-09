using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SesionesServicio.Aplicacion.Consultas.ObtenerProgresoSesion;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.PruebasUnitarias.Consultas;

public class ObtenerProgresoSesionManejadorPruebas
{
    private static readonly Guid SesionId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Participante1 = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Participante2 = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static ObtenerProgresoSesionManejador Construir(
        IReadOnlyList<ProgresoTriviaItem>? triviaItems = null,
        IReadOnlyList<ProgresoTesoroItem>? tesoroItems = null)
    {
        var repoTrivia = new Mock<IRepositorioRespuestasTrivia>();
        repoTrivia.Setup(r => r.ObtenerProgresoTriviaAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(triviaItems ?? Array.Empty<ProgresoTriviaItem>());

        var repoTesoro = new Mock<IRepositorioEvidenciasTesoro>();
        repoTesoro.Setup(r => r.ObtenerProgresoTesoroAsync(SesionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tesoroItems ?? Array.Empty<ProgresoTesoroItem>());

        return new ObtenerProgresoSesionManejador(repoTrivia.Object, repoTesoro.Object);
    }

    private static Task<IReadOnlyList<Commons.Dtos.ProgresoSesionParticipanteDto>> Ejecutar(
        IReadOnlyList<ProgresoTriviaItem>? trivia = null,
        IReadOnlyList<ProgresoTesoroItem>? tesoro = null)
        => Construir(trivia, tesoro)
            .Handle(new ObtenerProgresoSesionConsulta(SesionId), CancellationToken.None);

    [Fact]
    public async Task SinRespuestasNiEvidencias_DevuelveListaVacia()
    {
        var resultado = await Ejecutar();

        resultado.Should().BeEmpty();
    }

    [Fact]
    public async Task SoloTrivia_DevuelveProgresoTrivia()
    {
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 5, Correctas: 3, PuntosGanados: 80)
        };

        var resultado = await Ejecutar(trivia: triviaItems);

        resultado.Should().HaveCount(1);
        var item = resultado[0];
        item.ParticipanteIdentidadId.Should().Be(Participante1);
        item.TriviaRespondidas.Should().Be(5);
        item.TriviaCorrectas.Should().Be(3);
        item.TriviaIncorrectas.Should().Be(2);
        item.TriviaPuntosGanados.Should().Be(80);
        item.TesoroIntentosEnviados.Should().Be(0);
        item.TesoroEtapasCompletadas.Should().Be(0);
        item.TesoroPuntosGanados.Should().Be(0);
        item.TotalPuntosGanados.Should().Be(80);
    }

    [Fact]
    public async Task SoloTesoro_DevuelveProgresoTesoro()
    {
        var tesoroItems = new List<ProgresoTesoroItem>
        {
            new(Participante1, TotalIntentados: 3, Validos: 2, PuntosGanados: 50)
        };

        var resultado = await Ejecutar(tesoro: tesoroItems);

        resultado.Should().HaveCount(1);
        var item = resultado[0];
        item.ParticipanteIdentidadId.Should().Be(Participante1);
        item.TriviaRespondidas.Should().Be(0);
        item.TriviaPuntosGanados.Should().Be(0);
        item.TesoroIntentosEnviados.Should().Be(3);
        item.TesoroEtapasCompletadas.Should().Be(2);
        item.TesoroPuntosGanados.Should().Be(50);
        item.TotalPuntosGanados.Should().Be(50);
    }

    [Fact]
    public async Task TriviaYTesoro_MismoParticipante_SumaPuntajes()
    {
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 4, Correctas: 4, PuntosGanados: 100)
        };
        var tesoroItems = new List<ProgresoTesoroItem>
        {
            new(Participante1, TotalIntentados: 2, Validos: 1, PuntosGanados: 60)
        };

        var resultado = await Ejecutar(trivia: triviaItems, tesoro: tesoroItems);

        resultado.Should().HaveCount(1);
        var item = resultado[0];
        item.TriviaPuntosGanados.Should().Be(100);
        item.TesoroPuntosGanados.Should().Be(60);
        item.TotalPuntosGanados.Should().Be(160);
    }

    [Fact]
    public async Task TriviaYTesoro_MismoEquipo_SumaPuntajesPorEquipo()
    {
        var equipoId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, equipoId, TotalRespondidas: 2, Correctas: 1, PuntosGanados: 40)
        };
        var tesoroItems = new List<ProgresoTesoroItem>
        {
            new(Participante2, equipoId, TotalIntentados: 1, Validos: 1, PuntosGanados: 60)
        };

        var resultado = await Ejecutar(trivia: triviaItems, tesoro: tesoroItems);

        resultado.Should().HaveCount(1);
        var item = resultado[0];
        item.EquipoId.Should().Be(equipoId);
        item.TotalPuntosGanados.Should().Be(100);
        item.TriviaPuntosGanados.Should().Be(40);
        item.TesoroPuntosGanados.Should().Be(60);
    }

    [Fact]
    public async Task ParticipantesSoloEnUno_OtroTipoEsCero()
    {
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 3, Correctas: 2, PuntosGanados: 40)
        };
        var tesoroItems = new List<ProgresoTesoroItem>
        {
            new(Participante2, TotalIntentados: 1, Validos: 1, PuntosGanados: 25)
        };

        var resultado = await Ejecutar(trivia: triviaItems, tesoro: tesoroItems);

        resultado.Should().HaveCount(2);

        var p1 = resultado.Single(x => x.ParticipanteIdentidadId == Participante1);
        p1.TriviaPuntosGanados.Should().Be(40);
        p1.TesoroPuntosGanados.Should().Be(0);
        p1.TotalPuntosGanados.Should().Be(40);

        var p2 = resultado.Single(x => x.ParticipanteIdentidadId == Participante2);
        p2.TriviaPuntosGanados.Should().Be(0);
        p2.TesoroPuntosGanados.Should().Be(25);
        p2.TotalPuntosGanados.Should().Be(25);
    }

    [Fact]
    public async Task MultipleParticipantesEnTrivia_DevuelveUnoPorParticipante()
    {
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 5, Correctas: 5, PuntosGanados: 150),
            new(Participante2, TotalRespondidas: 5, Correctas: 3, PuntosGanados: 90)
        };

        var resultado = await Ejecutar(trivia: triviaItems);

        resultado.Should().HaveCount(2);
        resultado.Should().Contain(x => x.ParticipanteIdentidadId == Participante1 && x.TotalPuntosGanados == 150);
        resultado.Should().Contain(x => x.ParticipanteIdentidadId == Participante2 && x.TotalPuntosGanados == 90);
    }

    [Fact]
    public async Task IncorrectasCalculadasComoRespondidas_MenosCorrectas()
    {
        var triviaItems = new List<ProgresoTriviaItem>
        {
            new(Participante1, TotalRespondidas: 10, Correctas: 7, PuntosGanados: 200)
        };

        var resultado = await Ejecutar(trivia: triviaItems);

        resultado[0].TriviaIncorrectas.Should().Be(3);
    }
}
