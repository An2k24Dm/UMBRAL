using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarTrivia;

public sealed class ModificarTriviaManejador : IRequestHandler<ModificarTriviaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IValidador<ModificarTriviaComando> _validador;

    public ModificarTriviaManejador(
        IRepositorioJuegos repositorio,
        IRepositorioMisiones repositorioMisiones,
        IValidador<ModificarTriviaComando> validador)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _validador = validador;
    }

    public async Task Handle(ModificarTriviaComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (await _repositorioMisiones.EsContenidoUsadoEnMisionActivaAsync(
                TipoModoDeJuego.Trivia, comando.TriviaId, cancelacion))
            throw new ContenidoUsadoEnMisionActivaExcepcion();

        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la trivia con ID '{comando.TriviaId}'.");

        trivia.ModificarDatos(
            comando.Dto.NuevoNombre,
            comando.Dto.NuevaDescripcion,
            comando.Dto.NuevoTiempoLimitePorPregunta);

        await _repositorio.ModificarDatosTriviaAsync(trivia, cancelacion);
    }
}
