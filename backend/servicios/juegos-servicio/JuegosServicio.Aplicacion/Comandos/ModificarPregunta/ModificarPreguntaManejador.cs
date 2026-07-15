using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarPregunta;

public sealed class ModificarPreguntaManejador : IRequestHandler<ModificarPreguntaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ModificarPreguntaManejador(
        IRepositorioJuegos repositorio,
        IRepositorioMisiones repositorioMisiones,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _registroLogs = registroLogs;
    }

    public async Task Handle(ModificarPreguntaComando comando, CancellationToken cancelacion)
    {
        if (await _repositorioMisiones.EsContenidoUsadoEnMisionActivaAsync(
                TipoModoDeJuego.Trivia, comando.TriviaId, cancelacion))
            throw new ContenidoUsadoEnMisionActivaExcepcion();

        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la trivia con ID '{comando.TriviaId}'.");

        var dto = comando.Datos;
        var nuevasOpciones = dto.NuevasOpciones.Select(o => (o.Texto, o.EsCorrecta));

        trivia.ModificarPregunta(
            comando.PreguntaId,
            dto.NuevoEnunciado,
            Tiempo.CrearParaPregunta(dto.NuevoTiempoEstimado),
            nuevasOpciones);

        var preguntaModificada = trivia.Preguntas.First(p => p.Id == comando.PreguntaId);
        await _repositorio.ModificarPreguntaAsync(trivia.Id, preguntaModificada, cancelacion);

        _registroLogs.Informacion(
            evento: "PreguntaModificada",
            descripcion: "Usuario modificó una pregunta correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["PreguntaId"] = comando.PreguntaId,
                ["TriviaId"] = comando.TriviaId
            });
    }
}
