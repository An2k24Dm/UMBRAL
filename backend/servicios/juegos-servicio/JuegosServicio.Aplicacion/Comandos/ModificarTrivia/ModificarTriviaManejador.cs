using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Aplicacion.Validaciones;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;
using MediatR;

namespace JuegosServicio.Aplicacion.Comandos.ModificarTrivia;

public sealed class ModificarTriviaManejador : IRequestHandler<ModificarTriviaComando>
{
    private readonly IRepositorioJuegos _repositorio;
    private readonly IRepositorioMisiones _repositorioMisiones;
    private readonly IValidador<ModificarTriviaComando> _validador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ModificarTriviaManejador(
        IRepositorioJuegos repositorio,
        IRepositorioMisiones repositorioMisiones,
        IValidador<ModificarTriviaComando> validador,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _repositorioMisiones = repositorioMisiones;
        _validador = validador;
        _registroLogs = registroLogs;
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
            Tiempo.CrearPositivo(comando.Dto.NuevoTiempoLimitePorPregunta));

        await _repositorio.ModificarDatosTriviaAsync(trivia, cancelacion);

        _registroLogs.Informacion(
            evento: "TriviaModificada",
            descripcion: "Usuario modificó una trivia correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["TriviaId"] = comando.TriviaId
            });
    }
}
