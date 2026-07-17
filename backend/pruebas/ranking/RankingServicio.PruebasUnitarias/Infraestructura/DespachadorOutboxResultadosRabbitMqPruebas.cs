using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RankingServicio.Infraestructura.Persistencia;
using RankingServicio.Infraestructura.RabbitMq;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Infraestructura;

public class DespachadorOutboxResultadosRabbitMqPruebas
{
    [Fact]
    public async Task DespacharPendientesAsync_PublicaMensajesPendientesYLosMarcaEnviados()
    {
        var id = Guid.NewGuid();
        await using var contexto = CrearContexto();
        contexto.OutboxRanking.Add(new OutboxMensajeRankingModelo
        {
            Id = id,
            RoutingKey = "ranking.puntaje_actualizado",
            PayloadJson = "{\"ok\":true}",
            CreadoEnUtc = DateTime.UtcNow.AddMinutes(-5),
            Estado = "Pendiente"
        });
        await contexto.SaveChangesAsync();
        var despachador = CrearDespachador(contexto);
        var canal = InyectarCanal(despachador);

        await InvocarDespacharPendientesAsync(despachador);

        var mensaje = await contexto.OutboxRanking.SingleAsync(m => m.Id == id);
        mensaje.Estado.Should().Be("Enviado");
        mensaje.EnviadoEnUtc.Should().NotBeNull();
        mensaje.UltimoError.Should().BeNull();
        canal.Verify(c => c.BasicPublish(
            "umbral.eventos",
            "ranking.puntaje_actualizado",
            true,
            It.Is<IBasicProperties>(p =>
                p.Persistent &&
                p.ContentType == "application/json" &&
                p.MessageId == id.ToString()),
            It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
        canal.Verify(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task DespacharPendientesAsync_ErrorDePublicacionReprogramaIntento()
    {
        await using var contexto = CrearContexto();
        var id = Guid.NewGuid();
        contexto.OutboxRanking.Add(new OutboxMensajeRankingModelo
        {
            Id = id,
            RoutingKey = "ranking.puntaje_actualizado",
            PayloadJson = "{\"ok\":true}",
            CreadoEnUtc = DateTime.UtcNow.AddMinutes(-5),
            Estado = "Pendiente",
            Intentos = 2
        });
        await contexto.SaveChangesAsync();
        var despachador = CrearDespachador(contexto);
        InyectarCanal(despachador, lanzarAlConfirmar: true);

        await InvocarDespacharPendientesAsync(despachador);

        var mensaje = await contexto.OutboxRanking.SingleAsync(m => m.Id == id);
        mensaje.Estado.Should().Be("Pendiente");
        mensaje.Intentos.Should().Be(3);
        mensaje.UltimoError.Should().Contain("confirmacion fallida");
        mensaje.ProximoIntentoUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task DespacharPendientesAsync_AlcanzarMaxIntentosMarcaFallido()
    {
        await using var contexto = CrearContexto();
        var id = Guid.NewGuid();
        contexto.OutboxRanking.Add(new OutboxMensajeRankingModelo
        {
            Id = id,
            RoutingKey = "ranking.puntaje_actualizado",
            PayloadJson = "{\"ok\":true}",
            CreadoEnUtc = DateTime.UtcNow.AddMinutes(-5),
            Estado = "Pendiente",
            Intentos = 7
        });
        await contexto.SaveChangesAsync();
        var despachador = CrearDespachador(contexto);
        InyectarCanal(despachador, lanzarAlConfirmar: true);

        await InvocarDespacharPendientesAsync(despachador);

        var mensaje = await contexto.OutboxRanking.SingleAsync(m => m.Id == id);
        mensaje.Estado.Should().Be("Fallido");
        mensaje.Intentos.Should().Be(8);
        mensaje.UltimoError.Should().Contain("confirmacion fallida");
    }

    [Fact]
    public async Task DespacharPendientesAsync_IgnoraNoPendientesYFuturos()
    {
        await using var contexto = CrearContexto();
        contexto.OutboxRanking.AddRange(
            new OutboxMensajeRankingModelo
            {
                Id = Guid.NewGuid(),
                RoutingKey = "ranking.puntaje_actualizado",
                PayloadJson = "{}",
                CreadoEnUtc = DateTime.UtcNow.AddMinutes(-5),
                Estado = "Enviado"
            },
            new OutboxMensajeRankingModelo
            {
                Id = Guid.NewGuid(),
                RoutingKey = "ranking.puntaje_actualizado",
                PayloadJson = "{}",
                CreadoEnUtc = DateTime.UtcNow.AddMinutes(-4),
                Estado = "Pendiente",
                ProximoIntentoUtc = DateTime.UtcNow.AddHours(1)
            });
        await contexto.SaveChangesAsync();
        var despachador = CrearDespachador(contexto);
        var canal = InyectarCanal(despachador);

        await InvocarDespacharPendientesAsync(despachador);

        canal.Verify(c => c.BasicPublish(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<IBasicProperties>(),
            It.IsAny<ReadOnlyMemory<byte>>()), Times.Never);
    }

    private static ContextoRanking CrearContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoRanking>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ContextoRanking(opciones);
    }

    private static DespachadorOutboxResultadosRabbitMq CrearDespachador(ContextoRanking contexto)
    {
        var servicios = new ServiceCollection()
            .AddSingleton(contexto)
            .BuildServiceProvider();

        return new DespachadorOutboxResultadosRabbitMq(
            servicios.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OpcionesRabbitMq()),
            NullLogger<DespachadorOutboxResultadosRabbitMq>.Instance);
    }

    private static Mock<IModel> InyectarCanal(
        DespachadorOutboxResultadosRabbitMq despachador,
        bool lanzarAlConfirmar = false)
    {
        var propiedades = new Mock<IBasicProperties>();
        propiedades.SetupAllProperties();
        var canal = new Mock<IModel>();
        canal.SetupGet(c => c.IsOpen).Returns(true);
        canal.Setup(c => c.CreateBasicProperties()).Returns(propiedades.Object);
        if (lanzarAlConfirmar)
            canal.Setup(c => c.WaitForConfirmsOrDie(It.IsAny<TimeSpan>()))
                .Throws(new InvalidOperationException("confirmacion fallida"));
        typeof(DespachadorOutboxResultadosRabbitMq)
            .GetField("_canal", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(despachador, canal.Object);
        return canal;
    }

    private static async Task InvocarDespacharPendientesAsync(
        DespachadorOutboxResultadosRabbitMq despachador)
    {
        var metodo = typeof(DespachadorOutboxResultadosRabbitMq).GetMethod(
            "DespacharPendientesAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var tarea = (Task)metodo.Invoke(
            despachador, new object[] { CancellationToken.None })!;
        await tarea;
    }
}
