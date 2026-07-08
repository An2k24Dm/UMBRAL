using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IClienteJuegosTrivia
{
    Task<TriviaParticipanteJuegosDto?> ObtenerTriviaParticipanteAsync(
        Guid triviaId, CancellationToken cancelacion);

    Task<VerificacionRespuestaJuegosDto?> VerificarRespuestaAsync(
        Guid triviaId, Guid preguntaId, Guid opcionSeleccionadaId, CancellationToken cancelacion);
}
