using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class AgregarPreguntaManejador : IRequestHandler<AgregarPreguntaComando, Guid>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IValidador<AgregarPreguntaComando> _validador;
    private readonly ILogger<AgregarPreguntaManejador> _registro;

    public AgregarPreguntaManejador(
        IRepositorioJuegos repositorio,
        IRepositorioMisiones repositorioMisiones,
        IValidador<AgregarPreguntaComando> validador,
        ILogger<AgregarPreguntaManejador> registro)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _validador = validador;
        _registro = registro;
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

        var pregunta = trivia.AgregarPregunta(dto.Enunciado, dto.PuntajeAsignado, dto.TiempoEstimado, opciones);

        await _repositorio.AgregarPreguntaAsync(trivia.Id, pregunta, cancelacion);

        _registro.LogInformation(
            "Pregunta (ID: {PreguntaId}) agregada a la trivia {TriviaId}.",
            pregunta.Id, trivia.Id);

        return pregunta.Id;
    }
}
