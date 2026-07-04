using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ActivarTrivia;

public sealed class ActivarTriviaManejador : IRequestHandler<ActivarTriviaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ActivarTriviaManejador(
        IRepositorioJuegos repositorio,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _registroLogs = registroLogs;
    }

    public async Task Handle(ActivarTriviaComando comando, CancellationToken cancelacion)
    {
        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la trivia con ID '{comando.TriviaId}'.");

        trivia.Activar();

        await _repositorio.ActivarTriviaAsync(trivia, cancelacion);

        _registroLogs.Informacion(
            evento: "TriviaActivada",
            descripcion: "Usuario activó una trivia correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["TriviaId"] = comando.TriviaId
            });
    }
}
