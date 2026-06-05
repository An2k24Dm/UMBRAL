using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ModificarPreguntaManejador : IRequestHandler<ModificarPreguntaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly ILogger<ModificarPreguntaManejador> _registro;

    public ModificarPreguntaManejador(
        IRepositorioJuegos repositorio,
        IRepositorioMisiones repositorioMisiones,
        ILogger<ModificarPreguntaManejador> registro)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _registro = registro;
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

        trivia.ModificarPregunta(comando.PreguntaId, dto.NuevoEnunciado, dto.NuevoTiempoEstimado, nuevasOpciones);

        var preguntaModificada = trivia.Preguntas.First(p => p.Id == comando.PreguntaId);
        await _repositorio.ModificarPreguntaAsync(trivia.Id, preguntaModificada, cancelacion);

        _registro.LogInformation(
            "Pregunta {PreguntaId} modificada en la trivia {TriviaId}.",
            comando.PreguntaId, comando.TriviaId);
    }
}
