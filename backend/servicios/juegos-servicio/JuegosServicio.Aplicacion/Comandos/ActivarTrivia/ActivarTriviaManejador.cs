using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ActivarTrivia;

public sealed class ActivarTriviaManejador : IRequestHandler<ActivarTriviaComando>
{
    private readonly IRepositorioJuegos _repositorio;

    public ActivarTriviaManejador(IRepositorioJuegos repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(ActivarTriviaComando comando, CancellationToken cancelacion)
    {
        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la trivia con ID '{comando.TriviaId}'.");

        trivia.Activar();

        await _repositorio.ActivarTriviaAsync(trivia, cancelacion);
    }
}
