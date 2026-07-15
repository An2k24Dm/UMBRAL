using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.EliminarPregunta;

public sealed class EliminarPreguntaManejador : IRequestHandler<EliminarPreguntaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public EliminarPreguntaManejador(
        IRepositorioJuegos repositorio,
        IRepositorioMisiones repositorioMisiones,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _registroLogs = registroLogs;
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

        _registroLogs.Informacion(
            evento: "PreguntaEliminada",
            descripcion: "Usuario eliminó una pregunta correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["PreguntaId"] = comando.PreguntaId,
                ["TriviaId"] = comando.TriviaId
            });
    }
}
