using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class EliminarPreguntaManejador : IRequestHandler<EliminarPreguntaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly ILogger<EliminarPreguntaManejador> _registro;

    public EliminarPreguntaManejador(
        IRepositorioJuegos repositorio,
        IRepositorioMisiones repositorioMisiones,
        ILogger<EliminarPreguntaManejador> registro)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _registro = registro;
    }

    public async Task Handle(EliminarPreguntaComando comando, CancellationToken cancelacion)
    {
        if (await _repositorioMisiones.EsContenidoUsadoEnMisionActivaAsync(
                TipoModoDeJuego.Trivia, comando.TriviaId, cancelacion))
            throw new ContenidoUsadoEnMisionActivaExcepcion();

        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la trivia con ID '{comando.TriviaId}'.");

        trivia.EliminarPregunta(comando.PreguntaId);

        await _repositorio.EliminarPreguntaAsync(trivia.Id, comando.PreguntaId, cancelacion);

        _registro.LogInformation(
            "Pregunta {PreguntaId} eliminada de la trivia {TriviaId}.",
            comando.PreguntaId, comando.TriviaId);
    }
}
