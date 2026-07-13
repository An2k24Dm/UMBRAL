namespace RankingServicio.Aplicacion.Puertos;

public interface IPublicadorResultadosPuntaje
{
    Task PublicarPuntajeActualizadoAsync(
        PuntajeCalculadoDto puntaje, CancellationToken cancelacion);
}
