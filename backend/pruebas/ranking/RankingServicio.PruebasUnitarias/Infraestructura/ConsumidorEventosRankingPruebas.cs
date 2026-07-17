using System.Reflection;
using System.Text;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RankingServicio.Aplicacion.Comandos.ProcesarEquipoCreado;
using RankingServicio.Aplicacion.Comandos.ProcesarEvidenciaTesoro;
using RankingServicio.Aplicacion.Comandos.ProcesarParticipanteUnido;
using RankingServicio.Aplicacion.Comandos.ProcesarPenalizacion;
using RankingServicio.Aplicacion.Comandos.ProcesarRespuestaTrivia;
using RankingServicio.Infraestructura.RabbitMq;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Infraestructura;

public class ConsumidorEventosRankingPruebas
{
    [Fact]
    public async Task ProcesarAsync_RespuestaTrivia_EnviaComandoMediatR()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var eventoId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();

        await InvocarProcesarAsync(consumidor, "sesion.respuesta_trivia", $$"""
        {
          "EventoId":"{{eventoId}}",
          "SesionId":"{{sesionId}}",
          "MisionId":"{{Guid.NewGuid()}}",
          "EtapaId":"{{Guid.NewGuid()}}",
          "ParticipanteSesionId":"{{participanteId}}",
          "ParticipanteIdentidadId":"{{Guid.NewGuid()}}",
          "EquipoId":null,
          "TriviaId":"{{Guid.NewGuid()}}",
          "PreguntaId":"{{Guid.NewGuid()}}",
          "EsCorrecta":true,
          "PuntajeBase":50,
          "TiempoTardadoMs":1000,
          "TiempoLimiteMs":10000
        }
        """);

        mediator.Verify(m => m.Send(
            It.Is<ProcesarRespuestaTriviaComando>(c =>
                c.EventoId == eventoId
                && c.SesionId == sesionId
                && c.ParticipanteSesionId == participanteId
                && c.EsCorrecta
                && c.PuntajeBase == 50),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarAsync_EvidenciaTesoro_EnviaComandoMediatR()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var eventoId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        await InvocarProcesarAsync(consumidor, "sesion.evidencia_tesoro", $$"""
        {
          "EventoId":"{{eventoId}}",
          "SesionId":"{{Guid.NewGuid()}}",
          "MisionId":"{{Guid.NewGuid()}}",
          "EtapaId":"{{Guid.NewGuid()}}",
          "ParticipanteSesionId":"{{Guid.NewGuid()}}",
          "ParticipanteIdentidadId":"{{Guid.NewGuid()}}",
          "EquipoId":"{{equipoId}}",
          "BusquedaId":"{{Guid.NewGuid()}}",
          "EsValida":true,
          "PuntajeBase":70,
          "OrdenResolucion":2,
          "TotalCompetidores":5,
          "TiempoTranscurridoMs":3000,
          "TiempoLimiteMs":60000
        }
        """);

        mediator.Verify(m => m.Send(
            It.Is<ProcesarEvidenciaTesoroComando>(c =>
                c.EventoId == eventoId
                && c.EquipoId == equipoId
                && c.EsValida
                && c.OrdenResolucion == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarAsync_ParticipanteUnido_EnviaComandoMediatR()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var participanteIdentidadId = Guid.NewGuid();

        await InvocarProcesarAsync(consumidor, "sesion.participante_unido", $$"""
        {
          "EventoId":"{{Guid.NewGuid()}}",
          "SesionId":"{{Guid.NewGuid()}}",
          "ParticipanteSesionId":"{{Guid.NewGuid()}}",
          "ParticipanteIdentidadId":"{{participanteIdentidadId}}",
          "EquipoId":null
        }
        """);

        mediator.Verify(m => m.Send(
            It.Is<ProcesarParticipanteUnidoComando>(c =>
                c.ParticipanteIdentidadId == participanteIdentidadId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarAsync_EquipoCreado_EnviaComandoMediatR()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var equipoId = Guid.NewGuid();

        await InvocarProcesarAsync(consumidor, "sesion.equipo_creado", $$"""
        {
          "EventoId":"{{Guid.NewGuid()}}",
          "SesionId":"{{Guid.NewGuid()}}",
          "EquipoId":"{{equipoId}}"
        }
        """);

        mediator.Verify(m => m.Send(
            It.Is<ProcesarEquipoCreadoComando>(c => c.EquipoId == equipoId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarAsync_PenalizacionParticipante_EnviaComandoMediatR()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var eventoId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();

        await InvocarProcesarAsync(consumidor, "sesion.penalizacion_aplicada", $$"""
        {
          "EventoId":"{{eventoId}}",
          "SesionId":"{{Guid.NewGuid()}}",
          "TipoObjetivo":"Participante",
          "ParticipanteSesionId":"{{participanteSesionId}}",
          "ParticipanteIdentidadId":"{{Guid.NewGuid()}}",
          "EquipoId":null,
          "Puntos":5,
          "Motivo":"Incumplimiento de una regla",
          "OperadorIdentidadId":"{{Guid.NewGuid()}}",
          "AplicadaEnUtc":"2026-07-17T12:00:00Z"
        }
        """);

        mediator.Verify(m => m.Send(
            It.Is<ProcesarPenalizacionComando>(c =>
                c.EventoId == eventoId
                && c.TipoObjetivo == "Participante"
                && c.ParticipanteSesionId == participanteSesionId
                && c.EquipoId == null
                && c.Puntos == 5),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarAsync_PenalizacionEquipo_EnviaComandoMediatR()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var equipoId = Guid.NewGuid();

        await InvocarProcesarAsync(consumidor, "sesion.penalizacion_aplicada", $$"""
        {
          "EventoId":"{{Guid.NewGuid()}}",
          "SesionId":"{{Guid.NewGuid()}}",
          "TipoObjetivo":"Equipo",
          "ParticipanteSesionId":null,
          "ParticipanteIdentidadId":null,
          "EquipoId":"{{equipoId}}",
          "Puntos":100,
          "Motivo":"Penalización grupal",
          "OperadorIdentidadId":"{{Guid.NewGuid()}}",
          "AplicadaEnUtc":"2026-07-17T12:00:00Z"
        }
        """);

        mediator.Verify(m => m.Send(
            It.Is<ProcesarPenalizacionComando>(c =>
                c.TipoObjetivo == "Equipo"
                && c.EquipoId == equipoId
                && c.ParticipanteSesionId == null
                && c.Puntos == 100),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcesarAsync_RoutingDesconocido_NoEnviaComandos()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);

        await InvocarProcesarAsync(consumidor, "desconocido", "{}");

        mediator.Verify(m => m.Send(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnMensajeRecibidoAsync_MensajeValidoHaceAck()
    {
        var mediator = new Mock<IMediator>();
        var consumidor = CrearConsumidor(mediator);
        var canal = InyectarCanal(consumidor);

        await InvocarOnMensajeRecibidoAsync(consumidor, new BasicDeliverEventArgs
        {
            RoutingKey = "sesion.equipo_creado",
            DeliveryTag = 77,
            Body = Encoding.UTF8.GetBytes($$"""
            {
              "EventoId":"{{Guid.NewGuid()}}",
              "SesionId":"{{Guid.NewGuid()}}",
              "EquipoId":"{{Guid.NewGuid()}}"
            }
            """)
        });

        canal.Verify(c => c.BasicAck(77, false), Times.Once);
        canal.Verify(c => c.BasicNack(It.IsAny<ulong>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task OnMensajeRecibidoAsync_PrimerErrorHaceNackConRequeue()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<ProcesarEquipoCreadoComando>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo transitorio"));
        var consumidor = CrearConsumidor(mediator);
        var canal = InyectarCanal(consumidor);

        await InvocarOnMensajeRecibidoAsync(consumidor, new BasicDeliverEventArgs
        {
            RoutingKey = "sesion.equipo_creado",
            DeliveryTag = 78,
            Redelivered = false,
            Body = Encoding.UTF8.GetBytes($$"""
            {
              "EventoId":"{{Guid.NewGuid()}}",
              "SesionId":"{{Guid.NewGuid()}}",
              "EquipoId":"{{Guid.NewGuid()}}"
            }
            """)
        });

        canal.Verify(c => c.BasicNack(78, false, true), Times.Once);
        canal.Verify(c => c.BasicAck(It.IsAny<ulong>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task OnMensajeRecibidoAsync_ErrorReentregadoPublicaDeadLetterYAck()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<ProcesarEquipoCreadoComando>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo permanente"));
        var consumidor = CrearConsumidor(mediator);
        var canal = InyectarCanal(consumidor);

        await InvocarOnMensajeRecibidoAsync(consumidor, new BasicDeliverEventArgs
        {
            RoutingKey = "sesion.equipo_creado",
            DeliveryTag = 79,
            Redelivered = true,
            Body = Encoding.UTF8.GetBytes($$"""
            {
              "EventoId":"{{Guid.NewGuid()}}",
              "SesionId":"{{Guid.NewGuid()}}",
              "EquipoId":"{{Guid.NewGuid()}}"
            }
            """)
        });

        canal.Verify(c => c.BasicPublish(
            "",
            "ranking.eventos.dlq",
            false,
            It.Is<IBasicProperties>(p =>
                p.Persistent &&
                p.ContentType == "application/json" &&
                p.Headers.ContainsKey("x-error") &&
                p.Headers.ContainsKey("x-original-routing-key") &&
                p.Headers.ContainsKey("x-failed-at-utc")),
            It.IsAny<ReadOnlyMemory<byte>>()), Times.Once);
        canal.Verify(c => c.BasicAck(79, false), Times.Once);
    }

    private static ConsumidorEventosRanking CrearConsumidor(Mock<IMediator> mediator)
    {
        var servicios = new ServiceCollection()
            .AddSingleton(mediator.Object)
            .BuildServiceProvider();

        return new ConsumidorEventosRanking(
            servicios.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new OpcionesRabbitMq()),
            NullLogger<ConsumidorEventosRanking>.Instance);
    }

    private static async Task InvocarProcesarAsync(
        ConsumidorEventosRanking consumidor,
        string routingKey,
        string cuerpo)
    {
        var metodo = typeof(ConsumidorEventosRanking).GetMethod(
            "ProcesarAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var tarea = (Task)metodo.Invoke(consumidor, new object[] { routingKey, cuerpo })!;
        await tarea;
    }

    private static async Task InvocarOnMensajeRecibidoAsync(
        ConsumidorEventosRanking consumidor,
        BasicDeliverEventArgs args)
    {
        var metodo = typeof(ConsumidorEventosRanking).GetMethod(
            "OnMensajeRecibidoAsync",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
        var tarea = (Task)metodo.Invoke(consumidor, new object[] { new object(), args })!;
        await tarea;
    }

    private static Mock<IModel> InyectarCanal(ConsumidorEventosRanking consumidor)
    {
        var propiedades = new Mock<IBasicProperties>();
        propiedades.SetupAllProperties();
        var canal = new Mock<IModel>();
        canal.Setup(c => c.CreateBasicProperties()).Returns(propiedades.Object);
        typeof(ConsumidorEventosRanking)
            .GetField("_canal", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(consumidor, canal.Object);
        return canal;
    }
}
