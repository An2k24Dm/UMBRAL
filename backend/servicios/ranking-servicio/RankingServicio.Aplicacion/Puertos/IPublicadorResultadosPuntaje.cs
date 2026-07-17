using RankingServicio.Commons.Dtos.Eventos.Salida;

namespace RankingServicio.Aplicacion.Puertos;

public interface IPublicadorResultadosPuntaje
{
    Task PublicarPuntajeActualizadoAsync(
        PuntajeCalculadoDto puntaje, CancellationToken cancelacion);

    Task PublicarPenalizacionProcesadaAsync(
        PenalizacionProcesadaDto penalizacion, CancellationToken cancelacion);
}
