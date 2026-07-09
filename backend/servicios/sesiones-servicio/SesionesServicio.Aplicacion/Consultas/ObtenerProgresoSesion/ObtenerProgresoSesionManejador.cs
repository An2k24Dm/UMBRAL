using MediatR;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerProgresoSesion;

public sealed class ObtenerProgresoSesionManejador
    : IRequestHandler<ObtenerProgresoSesionConsulta, IReadOnlyList<ProgresoSesionParticipanteDto>>
{
    private readonly IRepositorioRespuestasTrivia _repositorioTrivia;
    private readonly IRepositorioEvidenciasTesoro _repositorioTesoro;

    public ObtenerProgresoSesionManejador(
        IRepositorioRespuestasTrivia repositorioTrivia,
        IRepositorioEvidenciasTesoro repositorioTesoro)
    {
        _repositorioTrivia = repositorioTrivia;
        _repositorioTesoro = repositorioTesoro;
    }

    public async Task<IReadOnlyList<ProgresoSesionParticipanteDto>> Handle(
        ObtenerProgresoSesionConsulta consulta, CancellationToken cancelacion)
    {
        // Las queries deben ser secuenciales: DbContext es scoped y no soporta
        // operaciones concurrentes en la misma instancia.
        var triviaItems = await _repositorioTrivia.ObtenerProgresoTriviaAsync(consulta.SesionId, cancelacion);
        var tesoroItems = await _repositorioTesoro.ObtenerProgresoTesoroAsync(consulta.SesionId, cancelacion);

        var todos = triviaItems.Select(t => t.ParticipanteIdentidadId)
            .Union(tesoroItems.Select(t => t.ParticipanteIdentidadId))
            .Distinct();

        var triviaDict = triviaItems.ToDictionary(t => t.ParticipanteIdentidadId);
        var tesoroDict = tesoroItems.ToDictionary(t => t.ParticipanteIdentidadId);

        return todos.Select(pid =>
        {
            triviaDict.TryGetValue(pid, out var trivia);
            tesoroDict.TryGetValue(pid, out var tesoro);

            var triviaPuntos = trivia?.PuntosGanados ?? 0;
            var tesoroPuntos = tesoro?.PuntosGanados ?? 0;

            return new ProgresoSesionParticipanteDto
            {
                ParticipanteIdentidadId = pid,
                TriviaRespondidas = trivia?.TotalRespondidas ?? 0,
                TriviaCorrectas = trivia?.Correctas ?? 0,
                TriviaIncorrectas = (trivia?.TotalRespondidas ?? 0) - (trivia?.Correctas ?? 0),
                TriviaPuntosGanados = triviaPuntos,
                TesoroIntentosEnviados = tesoro?.TotalIntentados ?? 0,
                TesoroEtapasCompletadas = tesoro?.Validos ?? 0,
                TesoroPuntosGanados = tesoroPuntos,
                TotalPuntosGanados = triviaPuntos + tesoroPuntos
            };
        }).ToList().AsReadOnly();
    }
}
