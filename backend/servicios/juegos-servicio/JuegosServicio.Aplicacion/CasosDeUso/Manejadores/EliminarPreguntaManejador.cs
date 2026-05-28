using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class EliminarPreguntaManejador : IRequestHandler<EliminarPreguntaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly ILogger<EliminarPreguntaManejador> _registro;

    public EliminarPreguntaManejador(IRepositorioJuegos repositorio, ILogger<EliminarPreguntaManejador> registro)
    {
        _repositorio = repositorio;
        _registro = registro;
    }

    public async Task Handle(EliminarPreguntaComando comando, CancellationToken cancelacion)
    {
        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la trivia con ID '{comando.TriviaId}'.");

        // La validación de estado Borrador y la existencia de la pregunta ocurren en el dominio.
        trivia.EliminarPregunta(comando.PreguntaId);

        await _repositorio.EliminarPreguntaAsync(trivia.Id, comando.PreguntaId, cancelacion);

        _registro.LogInformation(
            "Pregunta {PreguntaId} eliminada de la trivia {TriviaId}.",
            comando.PreguntaId, comando.TriviaId);
    }
}
