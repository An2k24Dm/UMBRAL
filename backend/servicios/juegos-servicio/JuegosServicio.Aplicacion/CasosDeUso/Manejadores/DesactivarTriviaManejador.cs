using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class DesactivarTriviaManejador : IRequestHandler<DesactivarTriviaComando>
{
    private readonly IRepositorioJuegos _repositorio;

    public DesactivarTriviaManejador(IRepositorioJuegos repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(DesactivarTriviaComando comando, CancellationToken cancelacion)
    {
        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la trivia con ID '{comando.TriviaId}'.");

        trivia.Desactivar();
        await _repositorio.DesactivarTriviaAsync(trivia, cancelacion);
    }
}
