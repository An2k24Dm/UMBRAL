using System.Text.Json;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Commons.Dtos.Eventos.Salida;
using RankingServicio.Infraestructura.Persistencia;

namespace RankingServicio.Infraestructura.RabbitMq;

public sealed class PublicadorResultadosPuntajeOutbox : IPublicadorResultadosPuntaje
{
    private const string RoutingKeyPuntajeActualizado = "ranking.puntaje_actualizado";
    private const string RoutingKeyPenalizacionProcesada = "ranking.penalizacion_procesada";
    private readonly ContextoRanking _contexto;

    public PublicadorResultadosPuntajeOutbox(ContextoRanking contexto)
    {
        _contexto = contexto;
    }

    public async Task PublicarPuntajeActualizadoAsync(
        PuntajeCalculadoDto puntaje, CancellationToken cancelacion)
    {
        _contexto.OutboxRanking.Add(new OutboxMensajeRankingModelo
        {
            Id = puntaje.EventoIdOrigen,
            RoutingKey = RoutingKeyPuntajeActualizado,
            PayloadJson = JsonSerializer.Serialize(puntaje),
            CreadoEnUtc = DateTime.UtcNow,
            Estado = "Pendiente"
        });

        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task PublicarPenalizacionProcesadaAsync(
        PenalizacionProcesadaDto penalizacion, CancellationToken cancelacion)
    {
        _contexto.OutboxRanking.Add(new OutboxMensajeRankingModelo
        {
            Id = Guid.NewGuid(),
            RoutingKey = RoutingKeyPenalizacionProcesada,
            PayloadJson = JsonSerializer.Serialize(penalizacion),
            CreadoEnUtc = DateTime.UtcNow,
            Estado = "Pendiente"
        });

        await _contexto.SaveChangesAsync(cancelacion);
    }
}
