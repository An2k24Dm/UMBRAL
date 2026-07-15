using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.AgregarPregunta;

public sealed class AgregarPreguntaManejador : IRequestHandler<AgregarPreguntaComando, Guid>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IValidador<AgregarPreguntaComando> _validador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public AgregarPreguntaManejador(
        IRepositorioJuegos repositorio,
        IRepositorioMisiones repositorioMisiones,
        IValidador<AgregarPreguntaComando> validador,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _validador = validador;
        _registroLogs = registroLogs;
    }

    public async Task<Guid> Handle(AgregarPreguntaComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (await _repositorioMisiones.EsContenidoUsadoEnMisionActivaAsync(
                TipoModoDeJuego.Trivia, comando.TriviaId, cancelacion))
            throw new ContenidoUsadoEnMisionActivaExcepcion();

        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la trivia con ID '{comando.TriviaId}'.");

        var dto = comando.Datos;
        var opciones = dto.Opciones.Select(o => (o.Texto, o.EsCorrecta));

        var pregunta = trivia.AgregarPregunta(
            dto.Enunciado,
            Puntaje.CrearParaPregunta(dto.PuntajeAsignado),
            Tiempo.CrearParaPregunta(dto.TiempoEstimado),
            opciones);

        await _repositorio.AgregarPreguntaAsync(trivia.Id, pregunta, cancelacion);

        _registroLogs.Informacion(
            evento: "PreguntaAgregada",
            descripcion: "Usuario agregó una pregunta correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["PreguntaId"] = pregunta.Id,
                ["TriviaId"] = trivia.Id
            });

        return pregunta.Id;
    }
}
