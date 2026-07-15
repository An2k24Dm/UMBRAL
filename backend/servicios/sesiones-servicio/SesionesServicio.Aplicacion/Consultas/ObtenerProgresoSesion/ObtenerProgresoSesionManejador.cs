using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerProgresoSesion;

public sealed class ObtenerProgresoSesionManejador
    : IRequestHandler<ObtenerProgresoSesionConsulta, ProgresoSesionDto>
{
    private const string TipoEtapaTrivia = "Trivia";

    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly IRepositorioRespuestasTrivia _repositorioTrivia;
    private readonly IRepositorioEvidenciasTesoro _repositorioTesoro;
    private readonly IClienteJuegosTrivia _clienteTrivia;

    public ObtenerProgresoSesionManejador(
        IRepositorioSesiones repositorioSesiones,
        IRepositorioRespuestasTrivia repositorioTrivia,
        IRepositorioEvidenciasTesoro repositorioTesoro,
        IClienteJuegosTrivia clienteTrivia)
    {
        _repositorioSesiones = repositorioSesiones;
        _repositorioTrivia = repositorioTrivia;
        _repositorioTesoro = repositorioTesoro;
        _clienteTrivia = clienteTrivia;
    }

    public async Task<ProgresoSesionDto> Handle(
        ObtenerProgresoSesionConsulta consulta, CancellationToken cancelacion)
    {
        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(consulta.SesionId, cancelacion);
        var triviaItems = await _repositorioTrivia.ObtenerProgresoTriviaAsync(consulta.SesionId, cancelacion);
        var triviaEtapasItems = await _repositorioTrivia.ObtenerProgresoTriviaPorEtapaAsync(
            consulta.SesionId, cancelacion);
        var tesoroItems = await _repositorioTesoro.ObtenerProgresoTesoroAsync(consulta.SesionId, cancelacion);
        var triviaEtapasCompletadas = await CalcularTriviaEtapasCompletadasAsync(
            sesion, triviaEtapasItems, cancelacion);

        var todos = triviaItems.Select(ClaveTrivia)
            .Union(tesoroItems.Select(ClaveTesoro))
            .Union(triviaEtapasCompletadas.Keys)
            .Distinct();

        var triviaDict = triviaItems.ToDictionary(ClaveTrivia);
        var tesoroDict = tesoroItems.ToDictionary(ClaveTesoro);

        var filas = todos.Select(clave =>
        {
            triviaDict.TryGetValue(clave, out var trivia);
            tesoroDict.TryGetValue(clave, out var tesoro);
            triviaEtapasCompletadas.TryGetValue(clave, out var etapasCompletadas);

            var participanteId = trivia?.ParticipanteIdentidadId
                ?? tesoro?.ParticipanteIdentidadId
                ?? triviaEtapasItems.FirstOrDefault(i => ClaveTriviaEtapa(i) == clave)?.ParticipanteIdentidadId
                ?? Guid.Empty;
            var equipoId = trivia?.EquipoId ?? tesoro?.EquipoId;
            if (!equipoId.HasValue)
                equipoId = triviaEtapasItems.FirstOrDefault(i => ClaveTriviaEtapa(i) == clave)?.EquipoId;

            return new ProgresoSesionParticipanteDto
            {
                ParticipanteIdentidadId = participanteId,
                EquipoId = equipoId,
                TriviaEtapasCompletadas = etapasCompletadas,
                TriviaRespondidas = trivia?.TotalRespondidas ?? 0,
                TriviaCorrectas = trivia?.Correctas ?? 0,
                TriviaIncorrectas = (trivia?.TotalRespondidas ?? 0) - (trivia?.Correctas ?? 0),
                TesoroIntentosEnviados = tesoro?.TotalIntentados ?? 0,
                TesoroEtapasCompletadas = tesoro?.Validos ?? 0
            };
        }).ToList();

        var ubicacion = ObtenerUbicacionActual(sesion);
        return new ProgresoSesionDto
        {
            MisionActualId = ubicacion?.MisionId,
            EtapaActualId = ubicacion?.EtapaId,
            OrdenMisionActual = ubicacion?.OrdenMision,
            OrdenEtapaActual = ubicacion?.OrdenEtapa,
            TipoEtapaActual = ubicacion?.TipoEtapa,
            FaseEtapaActual = ubicacion?.Fase.ToString(),
            Filas = filas
        };
    }

    private async Task<Dictionary<string, int>> CalcularTriviaEtapasCompletadasAsync(
        Sesion? sesion,
        IReadOnlyList<ProgresoTriviaEtapaItem> progresoPorEtapa,
        CancellationToken cancelacion)
    {
        if (sesion is null || progresoPorEtapa.Count == 0)
            return new Dictionary<string, int>();

        var etapasTrivia = sesion.SecuenciaEtapas
            .Where(e => string.Equals(e.TipoEtapa, TipoEtapaTrivia, StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (etapasTrivia.Count == 0)
            return new Dictionary<string, int>();

        var totales = await ObtenerTotalPreguntasPorEtapaAsync(etapasTrivia, cancelacion);
        return progresoPorEtapa
            .Where(item =>
                totales.TryGetValue(item.EtapaId, out var totalPreguntas)
                && totalPreguntas > 0
                && item.PreguntasRespondidas >= totalPreguntas)
            .GroupBy(ClaveTriviaEtapa)
            .ToDictionary(g => g.Key, g => g.Select(i => i.EtapaId).Distinct().Count());
    }

    private async Task<Dictionary<Guid, int>> ObtenerTotalPreguntasPorEtapaAsync(
        IReadOnlyList<EjecucionActualSesion> etapasTrivia,
        CancellationToken cancelacion)
    {
        var tareas = etapasTrivia.Select(async etapa =>
        {
            var trivia = await _clienteTrivia.ObtenerTriviaParticipanteAsync(
                etapa.ModoDeJuegoId, cancelacion);
            return new
            {
                etapa.EtapaId,
                Total = trivia?.Preguntas.Count ?? 0
            };
        });

        var resultados = await Task.WhenAll(tareas);
        return resultados
            .GroupBy(r => r.EtapaId)
            .ToDictionary(g => g.Key, g => g.First().Total);
    }

    private static EjecucionActualSesion? ObtenerUbicacionActual(Sesion? sesion)
    {
        if (sesion is null)
            return null;
        if (sesion.EjecucionActual is not null)
            return sesion.EjecucionActual;
        if (sesion.Estado == EstadoSesion.Finalizada)
            return sesion.SecuenciaEtapas
                .OrderByDescending(e => e.OrdenGlobal)
                .FirstOrDefault();
        return null;
    }

    private static string ClaveTrivia(ProgresoTriviaItem item)
        => item.EquipoId.HasValue
            ? $"e:{item.EquipoId.Value}"
            : $"p:{item.ParticipanteIdentidadId}";

    private static string ClaveTriviaEtapa(ProgresoTriviaEtapaItem item)
        => item.EquipoId.HasValue
            ? $"e:{item.EquipoId.Value}"
            : $"p:{item.ParticipanteIdentidadId}";

    private static string ClaveTesoro(ProgresoTesoroItem item)
        => item.EquipoId.HasValue
            ? $"e:{item.EquipoId.Value}"
            : $"p:{item.ParticipanteIdentidadId}";
}
