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
                TesoroIntentosEnviados = tesoro?.TotalIntentados ?? 0,
                TesoroEtapasCompletadas = tesoro?.Validos ?? 0
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
