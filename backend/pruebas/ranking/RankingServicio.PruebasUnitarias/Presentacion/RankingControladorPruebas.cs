using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RankingServicio.Aplicacion.Consultas.ObtenerRankingEquiposSesion;
using RankingServicio.Aplicacion.Consultas.ObtenerRankingGlobal;
using RankingServicio.Aplicacion.Consultas.ObtenerRankingParticipantesSesion;
using RankingServicio.Presentacion.Controladores;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Presentacion;

public sealed class RankingControladorPruebas
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly RankingController _controlador;

    public RankingControladorPruebas()
    {
        _controlador = new RankingController(_mediator.Object);
    }

    [Fact]
    public async Task ObtenerRankingParticipantes_enviaConsultaYRetornaOk()
    {
        var sesionId = Guid.NewGuid();
        var lista = new List<RankingParticipanteDto>();
        _mediator
            .Setup(m => m.Send(
                It.Is<ObtenerRankingParticipantesSesionConsulta>(c => c.SesionId == sesionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lista);

        var resultado = await _controlador.ObtenerRankingParticipantes(sesionId, CancellationToken.None);

        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(lista);
    }

    [Fact]
    public async Task ObtenerRankingEquipos_enviaConsultaYRetornaOk()
    {
        var sesionId = Guid.NewGuid();
        var lista = new List<RankingEquipoDto>();
        _mediator
            .Setup(m => m.Send(
                It.Is<ObtenerRankingEquiposSesionConsulta>(c => c.SesionId == sesionId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lista);

        var resultado = await _controlador.ObtenerRankingEquipos(sesionId, CancellationToken.None);

        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(lista);
    }

    [Fact]
    public async Task ObtenerRankingGlobal_enviaConsultaConTopYRetornaOk()
    {
        const int top = 25;
        var lista = new List<RankingGlobalDto>();
        _mediator
            .Setup(m => m.Send(
                It.Is<ObtenerRankingGlobalConsulta>(c => c.Top == top),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(lista);

        var resultado = await _controlador.ObtenerRankingGlobal(top, CancellationToken.None);

        var ok = resultado.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeSameAs(lista);
    }
}
