using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Dominio.Excepciones;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ModificarTriviaManejador : IRequestHandler<ModificarTriviaComando>
{
    private readonly IRepositorioJuegos _repositorio;

    public ModificarTriviaManejador(IRepositorioJuegos repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task Handle(ModificarTriviaComando comando, CancellationToken cancelacion)
    {
        var trivia = await _repositorio.ObtenerTriviaPorIdAsync(comando.TriviaId, cancelacion)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la trivia con ID '{comando.TriviaId}'.");

        trivia.ModificarDatos(
            comando.Dto.NuevoNombre,
            comando.Dto.NuevaDescripcion,
            comando.Dto.NuevoTiempoLimitePorPregunta);

        await _repositorio.ModificarDatosTriviaAsync(trivia, cancelacion);
    }
}
