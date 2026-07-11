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

        var todos = triviaItems.Select(ClaveTrivia)
            .Union(tesoroItems.Select(ClaveTesoro))
            .Distinct();

        var triviaDict = triviaItems.ToDictionary(ClaveTrivia);
        var tesoroDict = tesoroItems.ToDictionary(ClaveTesoro);

        return todos.Select(clave =>
        {
            triviaDict.TryGetValue(clave, out var trivia);
            tesoroDict.TryGetValue(clave, out var tesoro);

            var triviaPuntos = trivia?.PuntosGanados ?? 0;
            var tesoroPuntos = tesoro?.PuntosGanados ?? 0;
            var participanteId = trivia?.ParticipanteIdentidadId
                ?? tesoro?.ParticipanteIdentidadId
                ?? Guid.Empty;
            var equipoId = trivia?.EquipoId ?? tesoro?.EquipoId;

            return new ProgresoSesionParticipanteDto
            {
                ParticipanteIdentidadId = participanteId,
                EquipoId = equipoId,
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

    private static string ClaveTrivia(ProgresoTriviaItem item)
        => item.EquipoId.HasValue
            ? $"e:{item.EquipoId.Value}"
            : $"p:{item.ParticipanteIdentidadId}";

    private static string ClaveTesoro(ProgresoTesoroItem item)
        => item.EquipoId.HasValue
            ? $"e:{item.EquipoId.Value}"
            : $"p:{item.ParticipanteIdentidadId}";
}
