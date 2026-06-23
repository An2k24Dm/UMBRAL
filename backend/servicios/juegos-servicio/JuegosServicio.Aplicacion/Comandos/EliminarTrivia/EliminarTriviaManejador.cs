using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarTrivia;

public sealed class EliminarTriviaManejador : IRequestHandler<EliminarTriviaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;

    public EliminarTriviaManejador(IRepositorioJuegos repositorio, IRepositorioMisiones repositorioMisiones)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
    }

    public async Task Handle(EliminarTriviaComando comando, CancellationToken cancelacion)
    {
        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la trivia con ID '{comando.TriviaId}'.");

        if (trivia.Estado != EstadoTrivia.Inactiva)
            throw new ExcepcionDominio("Solo se pueden eliminar trivias en estado Inactiva.");

        if (await _repositorioMisiones.EsContenidoUsadoEnEtapaAsync(TipoModoDeJuego.Trivia, comando.TriviaId, cancelacion))
            throw new ExcepcionDominio("No se puede eliminar la trivia porque está asignada a una o más misiones.");

        await _repositorio.EliminarTriviaAsync(comando.TriviaId, cancelacion);
    }
}
