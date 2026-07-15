using MediatR;
using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingGlobal;

public sealed class ObtenerRankingGlobalManejador
    : IRequestHandler<ObtenerRankingGlobalConsulta, List<RankingGlobalDto>>
{
    private const int TopMaximo = 100;

    private readonly IConsultasRanking _consultas;
    private readonly IClienteIdentidadParticipantes _clienteIdentidad;

    public ObtenerRankingGlobalManejador(
        IConsultasRanking consultas,
        IClienteIdentidadParticipantes clienteIdentidad)
    {
        _consultas = consultas;
        _clienteIdentidad = clienteIdentidad;
    }

    public async Task<List<RankingGlobalDto>> Handle(
        ObtenerRankingGlobalConsulta consulta, CancellationToken cancelacion)
    {
        var top = Math.Clamp(consulta.Top <= 0 ? 50 : consulta.Top, 1, TopMaximo);
        var proyeccion = await _consultas.ObtenerRankingGlobalAsync(top, cancelacion);

        var datos = await _clienteIdentidad.ObtenerParticipantesPorIdsAsync(
            proyeccion.Select(p => p.ParticipanteIdentidadId), cancelacion);

        return proyeccion
            .Select((p, indice) => new RankingGlobalDto(
                indice + 1,
                p.ParticipanteIdentidadId,
                ResolucionAlias.Resolver(p.ParticipanteIdentidadId, datos),
                p.Puntaje))
            .ToList();
    }
}
