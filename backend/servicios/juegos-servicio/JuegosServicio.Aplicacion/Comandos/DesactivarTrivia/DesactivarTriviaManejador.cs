using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.DesactivarTrivia;

public sealed class DesactivarTriviaManejador : IRequestHandler<DesactivarTriviaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public DesactivarTriviaManejador(
        IRepositorioJuegos repositorio,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _registroLogs = registroLogs;
    }

    public async Task Handle(DesactivarTriviaComando comando, CancellationToken cancelacion)
    {
        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado(
                $"No se encontró la trivia con ID '{comando.TriviaId}'.");

        trivia.Desactivar();
        await _repositorio.DesactivarTriviaAsync(trivia, cancelacion);

        _registroLogs.Informacion(
            evento: "TriviaDesactivada",
            descripcion: "Usuario desactivó una trivia correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["TriviaId"] = comando.TriviaId
            });
    }
}
