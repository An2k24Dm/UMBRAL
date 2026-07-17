using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SesionesServicio.Infraestructura.Persistencia;
using SesionesServicio.Infraestructura.ServiciosExternos;

namespace SesionesServicio.PruebasUnitarias.ServiciosExternos;

public class PublicadorEventosRankingOutboxPruebas
{
    [Fact]
    public async Task PublicarRespuestaTriviaRegistrada_EncolaMensajePendiente()
    {
        await using var contexto = NuevoContexto();
        var publicador = new PublicadorEventosRankingOutbox(contexto);
        var eventoId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var preguntaId = Guid.NewGuid();

        await publicador.PublicarRespuestaTriviaRegistradaAsync(
            eventoId,
            sesionId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            equipoId: null,
            Guid.NewGuid(),
            preguntaId,
            esCorrecta: true,
            puntajeBase: 50,
            tiempoTardadoMs: 1000,
            tiempoLimiteMs: 10000,
            CancellationToken.None);

        var mensaje = await contexto.OutboxRanking.SingleAsync();
        mensaje.Id.Should().Be(eventoId);
        mensaje.RoutingKey.Should().Be("sesion.respuesta_trivia");
        mensaje.Estado.Should().Be("Pendiente");
        mensaje.Intentos.Should().Be(0);
        var payload = JsonDocument.Parse(mensaje.PayloadJson).RootElement;
        payload.GetProperty("SesionId").GetGuid().Should().Be(sesionId);
        payload.GetProperty("PreguntaId").GetGuid().Should().Be(preguntaId);
        payload.GetProperty("EsCorrecta").GetBoolean().Should().BeTrue();
        payload.GetProperty("PuntajeBase").GetInt32().Should().Be(50);
    }

    [Fact]
    public async Task PublicarEvidenciaTesoroRegistrada_EncolaMensajeConOrden()
    {
        await using var contexto = NuevoContexto();
        var publicador = new PublicadorEventosRankingOutbox(contexto);
        var eventoId = Guid.NewGuid();
        var busquedaId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        await publicador.PublicarEvidenciaTesoroRegistradaAsync(
            eventoId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            equipoId,
            busquedaId,
            esValida: true,
            puntajeBase: 75,
            ordenResolucion: 2,
            totalCompetidores: 5,
            tiempoTranscurridoMs: 3000,
            tiempoLimiteMs: 60000,
            CancellationToken.None);

        var mensaje = await contexto.OutboxRanking.SingleAsync();
        mensaje.Id.Should().Be(eventoId);
        mensaje.RoutingKey.Should().Be("sesion.evidencia_tesoro");
        var payload = JsonDocument.Parse(mensaje.PayloadJson).RootElement;
        payload.GetProperty("BusquedaId").GetGuid().Should().Be(busquedaId);
        payload.GetProperty("EquipoId").GetGuid().Should().Be(equipoId);
        payload.GetProperty("OrdenResolucion").GetInt32().Should().Be(2);
        payload.GetProperty("TotalCompetidores").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task PublicarParticipanteUnidoSesion_GeneraEventoYEncolaMensaje()
    {
        await using var contexto = NuevoContexto();
        var publicador = new PublicadorEventosRankingOutbox(contexto);
        var sesionId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();
        var participanteIdentidadId = Guid.NewGuid();

        await publicador.PublicarParticipanteUnidoSesionAsync(
            sesionId, participanteSesionId, participanteIdentidadId, null, CancellationToken.None);

        var mensaje = await contexto.OutboxRanking.SingleAsync();
        mensaje.Id.Should().NotBe(Guid.Empty);
        mensaje.RoutingKey.Should().Be("sesion.participante_unido");
        var payload = JsonDocument.Parse(mensaje.PayloadJson).RootElement;
        payload.GetProperty("EventoId").GetGuid().Should().Be(mensaje.Id);
        payload.GetProperty("SesionId").GetGuid().Should().Be(sesionId);
        payload.GetProperty("ParticipanteSesionId").GetGuid().Should().Be(participanteSesionId);
        payload.GetProperty("ParticipanteIdentidadId").GetGuid().Should().Be(participanteIdentidadId);
    }

    [Fact]
    public async Task PublicarEquipoCreadoSesion_GeneraEventoYEncolaMensaje()
    {
        await using var contexto = NuevoContexto();
        var publicador = new PublicadorEventosRankingOutbox(contexto);
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();

        await publicador.PublicarEquipoCreadoSesionAsync(
            sesionId, equipoId, CancellationToken.None);

        var mensaje = await contexto.OutboxRanking.SingleAsync();
        mensaje.Id.Should().NotBe(Guid.Empty);
        mensaje.RoutingKey.Should().Be("sesion.equipo_creado");
        var payload = JsonDocument.Parse(mensaje.PayloadJson).RootElement;
        payload.GetProperty("EventoId").GetGuid().Should().Be(mensaje.Id);
        payload.GetProperty("SesionId").GetGuid().Should().Be(sesionId);
        payload.GetProperty("EquipoId").GetGuid().Should().Be(equipoId);
    }

    [Fact]
    public async Task PublicarPenalizacionAplicada_EncolaMensajeConContrato()
    {
        await using var contexto = NuevoContexto();
        var publicador = new PublicadorEventosRankingOutbox(contexto);
        var eventoId = Guid.NewGuid();
        var penalizacionId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();
        var participanteIdentidadId = Guid.NewGuid();
        var operadorId = Guid.NewGuid();

        await publicador.PublicarPenalizacionAplicadaAsync(
            eventoId, penalizacionId, sesionId, "Participante",
            participanteSesionId, participanteIdentidadId, null,
            5, "Incumplió una regla", operadorId,
            new DateTime(2026, 7, 17, 12, 0, 0, DateTimeKind.Utc),
            CancellationToken.None);

        var mensaje = await contexto.OutboxRanking.SingleAsync();
        mensaje.Id.Should().Be(eventoId);
        mensaje.RoutingKey.Should().Be("sesion.penalizacion_aplicada");
        mensaje.Estado.Should().Be("Pendiente");
        var payload = JsonDocument.Parse(mensaje.PayloadJson).RootElement;
        payload.GetProperty("PenalizacionId").GetGuid().Should().Be(penalizacionId);
        payload.GetProperty("SesionId").GetGuid().Should().Be(sesionId);
        payload.GetProperty("TipoObjetivo").GetString().Should().Be("Participante");
        payload.GetProperty("ParticipanteSesionId").GetGuid().Should().Be(participanteSesionId);
        payload.GetProperty("ParticipanteIdentidadId").GetGuid().Should().Be(participanteIdentidadId);
        payload.GetProperty("EquipoId").ValueKind.Should().Be(JsonValueKind.Null);
        payload.GetProperty("Puntos").GetInt32().Should().Be(5);
        payload.GetProperty("Motivo").GetString().Should().Be("Incumplió una regla");
        payload.GetProperty("OperadorIdentidadId").GetGuid().Should().Be(operadorId);
    }

    private static ContextoSesiones NuevoContexto()
    {
        var opciones = new DbContextOptionsBuilder<ContextoSesiones>()
            .UseInMemoryDatabase("outbox-ranking-" + Guid.NewGuid())
            .Options;
        return new ContextoSesiones(opciones);
    }
}
