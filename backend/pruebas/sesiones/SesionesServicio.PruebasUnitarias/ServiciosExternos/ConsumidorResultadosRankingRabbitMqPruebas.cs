using System.Reflection;
using System.Text;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SesionesServicio.Aplicacion.Comandos.AplicarPuntajeRanking;
using SesionesServicio.Aplicacion.Comandos.AplicarResultadoPenalizacionRanking;
using SesionesServicio.Infraestructura.ServiciosExternos;

namespace SesionesServicio.PruebasUnitarias.ServiciosExternos;

public class ConsumidorResultadosRankingRabbitMqPruebas
{
    [Fact]
    public async Task OnMensajeRecibidoAsync_ResultadoPuntaje_EnviaComandoAplicarPuntaje()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var eventoOrigen = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        await InvocarOnMensajeRecibidoAsync(consumidor, new BasicDeliverEventArgs
        {
            RoutingKey = "ranking.puntaje_actualizado",
            DeliveryTag = 10,
            Redelivered = false,
            Body = Encoding.UTF8.GetBytes($$"""
            {
              "EventoIdOrigen":"{{eventoOrigen}}",
              "SesionId":"{{Guid.NewGuid()}}",
              "ParticipanteSesionId":"{{Guid.NewGuid()}}",
              "ParticipanteIdentidadId":"{{Guid.NewGuid()}}",
              "EquipoId":"{{equipoId}}",
              "PuntajeGanado":25,
              "PuntajeTotalParticipante":100,
              "PuntajeTotalEquipo":300,
              "CalculadoEnUtc":"2026-07-16T14:00:00Z"
            }
            """)
        });

        mediator.Verify(m => m.Send(
            It.Is<AplicarPuntajeRankingComando>(c =>
                c.EventoIdOrigen == eventoOrigen
                && c.EquipoId == equipoId
                && c.PuntajeGanado == 25
                && c.PuntajeTotalEquipo == 300),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnMensajeRecibidoAsync_ResultadoPenalizacion_EnviaComandoAplicarResultado()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var canal = InyectarCanal(consumidor);
        var eventoOrigen = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        await InvocarOnMensajeRecibidoAsync(consumidor, new BasicDeliverEventArgs
        {
            RoutingKey = "ranking.penalizacion_procesada",
            DeliveryTag = 20,
            Redelivered = false,
            Body = Encoding.UTF8.GetBytes($$"""
            {
              "EventoIdOrigen":"{{eventoOrigen}}",
              "PenalizacionId":"{{Guid.NewGuid()}}",
              "SesionId":"{{Guid.NewGuid()}}",
              "TipoObjetivo":"Equipo",
              "ParticipanteSesionId":null,
              "ParticipanteIdentidadId":null,
              "EquipoId":"{{equipoId}}",
              "PuntosPenalizados":10,
              "PuntosPenalizadosAcumulados":20,
              "PuntajeTotalParticipante":null,
              "PuntajeTotalEquipo":60,
              "CalculadoEnUtc":"2026-07-17T12:00:00Z"
            }
            """)
        });

        mediator.Verify(m => m.Send(
            It.Is<AplicarResultadoPenalizacionRankingComando>(c =>
                c.EventoIdOrigen == eventoOrigen
                && c.TipoObjetivo == "Equipo"
                && c.EquipoId == equipoId
                && c.PuntosPenalizadosAcumulados == 20
                && c.PuntajeTotalEquipo == 60),
            It.IsAny<CancellationToken>()), Times.Once);
        canal.Verify(c => c.BasicAck(20, false), Times.Once);
    }

    [Fact]
    public async Task OnMensajeRecibidoAsync_RoutingKeyDesconocido_HaceAckSinComando()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var canal = InyectarCanal(consumidor);

        await InvocarOnMensajeRecibidoAsync(consumidor, new BasicDeliverEventArgs
        {
            RoutingKey = "ranking.desconocido",
            DeliveryTag = 21,
            Redelivered = false,
            Body = Encoding.UTF8.GetBytes("{}")
        });

        mediator.Verify(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        canal.Verify(c => c.BasicAck(21, false), Times.Once);
    }

    [Fact]
    public async Task OnMensajeRecibidoAsync_JsonInvalidoPrimerIntento_NoPropagaExcepcion()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var canal = InyectarCanal(consumidor);

        await InvocarOnMensajeRecibidoAsync(consumidor, new BasicDeliverEventArgs
        {
            RoutingKey = "ranking.puntaje_actualizado",
            DeliveryTag = 11,
            Redelivered = false,
            Body = Encoding.UTF8.GetBytes("{mal-json")
        });

        mediator.Verify(m => m.Send(
            It.IsAny<AplicarPuntajeRankingComando>(), It.IsAny<CancellationToken>()), Times.Never);
        canal.Verify(c => c.BasicNack(11, false, true), Times.Once);
    }

    [Fact]
    public async Task OnMensajeRecibidoAsync_ResultadoValidoHaceAck()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var canal = InyectarCanal(consumidor);

        await InvocarOnMensajeRecibidoAsync(consumidor, EventoValido(deliveryTag: 12, redelivered: false));

        canal.Verify(c => c.BasicAck(12, false), Times.Once);
        canal.Verify(c => c.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task OnMensajeRecibidoAsync_ErrorReentregadoPublicaDeadLetterYAck()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<AplicarPuntajeRankingComando>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo permanente"));
        var consumidor = CrearConsumidor(mediator);
        var canal = InyectarCanal(consumidor);

        await InvocarOnMensajeRecibidoAsync(consumidor, EventoValido(deliveryTag: 13, redelivered: true));

        canal.Verify(c => c.BasicPublish(
            "",
            "sesiones.ranking.resultados.dlq",
            false,
            It.Is<IBasicProperties>(p =>
                p.Persistent &&
                p.ContentType == "application/json" &&
                p.Headers.ContainsKey("x-error") &&
                p.Headers.ContainsKey("x-original-routing-key") &&
                p.Headers.ContainsKey("x-failed-at-utc")),
            It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
        canal.Verify(c => c.BasicAck(13, false), Times.Once);
    }

    private static ConsumidorResultadosRankingRabbitMq CrearConsumidor(Mock<IMediator> mediator)
    {
        var servicios = new ServiceCollection()
            .AddSingleton(mediator.Object)
            .BuildServiceProvider();

        return new ConsumidorResultadosRankingRabbitMq(
            servicios.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OpcionesRabbitMq()),
            NullLogger<ConsumidorResultadosRankingRabbitMq>.Instance);
    }

    private static async Task InvocarOnMensajeRecibidoAsync(
        ConsumidorResultadosRankingRabbitMq consumidor,
        BasicDeliverEventArgs args)
    {
        var metodo = typeof(ConsumidorResultadosRankingRabbitMq).GetMethod(
            "OnMensajeRecibidoAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var tarea = (Task)metodo.Invoke(consumidor, new object[] { new object(), args })!;
        await tarea;
    }

    private static Mock<IModel> InyectarCanal(ConsumidorResultadosRankingRabbitMq consumidor)
    {
        var propiedades = new Mock<IBasicProperties>();
        propiedades.SetupAllProperties();
        var canal = new Mock<IModel>();
        canal.Setup(c => c.CreateBasicProperties()).Returns(propiedades.Object);
        typeof(ConsumidorResultadosRankingRabbitMq)
            .GetField("_canal", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(consumidor, canal.Object);
        return canal;
    }

    private static BasicDeliverEventArgs EventoValido(ulong deliveryTag, bool redelivered) => new()
    {
        RoutingKey = "ranking.puntaje_actualizado",
        DeliveryTag = deliveryTag,
        Redelivered = redelivered,
        Body = Encoding.UTF8.GetBytes($$"""
        {
          "EventoIdOrigen":"{{Guid.NewGuid()}}",
          "SesionId":"{{Guid.NewGuid()}}",
          "ParticipanteSesionId":"{{Guid.NewGuid()}}",
          "ParticipanteIdentidadId":"{{Guid.NewGuid()}}",
          "EquipoId":"{{Guid.NewGuid()}}",
          "PuntajeGanado":25,
          "PuntajeTotalParticipante":100,
          "PuntajeTotalEquipo":300,
          "CalculadoEnUtc":"2026-07-16T14:00:00Z"
        }
        """)
    };
}
