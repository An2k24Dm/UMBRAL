using MediatR;
using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingParticipantesSesion;

public sealed class ObtenerRankingParticipantesSesionManejador
    : IRequestHandler<ObtenerRankingParticipantesSesionConsulta, List<RankingParticipanteDto>>
{
    private readonly IRepositorioRanking _repo;
    private readonly IClienteIdentidadParticipantes _clienteIdentidad;

    public ObtenerRankingParticipantesSesionManejador(
        IRepositorioRanking repo,
        IClienteIdentidadParticipantes clienteIdentidad)
    {
        _repo = repo;
        _clienteIdentidad = clienteIdentidad;
    }

    public async Task<List<RankingParticipanteDto>> Handle(
        ObtenerRankingParticipantesSesionConsulta consulta, CancellationToken cancelacion)
    {
        var ranking = await _repo.ObtenerPorSesionAsync(consulta.SesionId, cancelacion);
        if (ranking is null)
            return new List<RankingParticipanteDto>();

        var ordenados = ranking.ParticipantesOrdenados();

        var datos = await _clienteIdentidad.ObtenerParticipantesPorIdsAsync(
            ordenados.Select(p => p.ParticipanteIdentidadId), cancelacion);

        return ordenados
            .Select((p, indice) => new RankingParticipanteDto(
                indice + 1,
                p.ParticipanteSesionId,
                p.ParticipanteIdentidadId,
                p.EquipoId,
                ResolucionAlias.Resolver(p.ParticipanteIdentidadId, datos),
                p.Puntaje.Valor))
            .ToList();
    }
}
