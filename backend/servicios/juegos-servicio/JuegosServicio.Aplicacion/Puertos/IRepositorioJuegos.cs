using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;

namespace JuegosServicio.Aplicacion.Puertos;

public interface IRepositorioJuegos
{
    Task<bool> ExisteTriviaConNombreAsync(string nombre, CancellationToken cancelacion);
    Task AgregarTriviaAsync(Trivia trivia, CancellationToken cancelacion);

    Task<Trivia?> ObtenerTriviaPorIdAsync(Guid triviaId, CancellationToken cancelacion);
    Task AgregarPreguntaAsync(Guid triviaId, Pregunta pregunta, CancellationToken cancelacion);
    Task ModificarPreguntaAsync(Guid triviaId, Pregunta pregunta, CancellationToken cancelacion);
    Task EliminarPreguntaAsync(Guid triviaId, Guid preguntaId, CancellationToken cancelacion);

    Task ActivarTriviaAsync(Trivia trivia, CancellationToken cancelacion);
    Task ModificarDatosTriviaAsync(Trivia trivia, CancellationToken cancelacion);
    Task ArchivarTriviaAsync(Trivia trivia, CancellationToken cancelacion);

    Task<List<TriviaResumenDto>> ObtenerTriviasEnBorradorAsync(Guid creadorId, CancellationToken cancelacion);
    Task<TriviaDetalleDto?> ObtenerDetalleTriviaAsync(Guid triviaId, CancellationToken cancelacion);
    Task<List<TriviaResumenDto>> ObtenerTriviasActivasAsync(CancellationToken cancelacion);
}
