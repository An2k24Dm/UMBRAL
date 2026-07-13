using MediatR;
using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Aplicacion.Consultas.ObtenerRankingEquiposSesion;

public sealed class ObtenerRankingEquiposSesionManejador
    : IRequestHandler<ObtenerRankingEquiposSesionConsulta, List<RankingEquipoDto>>
{
    private readonly IRepositorioRanking _repo;
    private readonly IClienteIdentidadParticipantes _clienteIdentidad;
    private readonly IClienteSesionesRanking _clienteSesiones;

    public ObtenerRankingEquiposSesionManejador(
        IRepositorioRanking repo,
        IClienteIdentidadParticipantes clienteIdentidad,
        IClienteSesionesRanking clienteSesiones)
    {
        _repo = repo;
        _clienteIdentidad = clienteIdentidad;
        _clienteSesiones = clienteSesiones;
    }

    public async Task<List<RankingEquipoDto>> Handle(
        ObtenerRankingEquiposSesionConsulta consulta, CancellationToken cancelacion)
    {
        var ranking = await _repo.ObtenerPorSesionAsync(consulta.SesionId, cancelacion);
        if (ranking is null)
            return new List<RankingEquipoDto>();

        var equipos = ranking.EquiposOrdenados();
        if (equipos.Count == 0)
            return new List<RankingEquipoDto>();

        var nombresEquipos = await _clienteSesiones.ObtenerNombresEquiposAsync(
            consulta.SesionId, cancelacion);

        var datosParticipantes = await _clienteIdentidad.ObtenerParticipantesPorIdsAsync(
            ranking.Participantes
                .Where(p => p.EquipoId.HasValue)
                .Select(p => p.ParticipanteIdentidadId),
            cancelacion);

        var resultado = new List<RankingEquipoDto>(equipos.Count);
        for (var indice = 0; indice < equipos.Count; indice++)
        {
            var equipo = equipos[indice];

            var aportes = ranking.ParticipantesDeEquipo(equipo.EquipoId)
                .Select(p => new AporteParticipanteEquipoDto(
                    p.ParticipanteSesionId,
                    p.ParticipanteIdentidadId,
                    ResolucionAlias.Resolver(p.ParticipanteIdentidadId, datosParticipantes),
                    p.Puntaje.Valor))
                .ToList();

            var nombre = nombresEquipos.TryGetValue(equipo.EquipoId, out var n)
                         && !string.IsNullOrWhiteSpace(n)
                ? n
                : equipo.EquipoId.ToString();

            resultado.Add(new RankingEquipoDto(
                indice + 1,
                equipo.EquipoId,
                nombre,
                equipo.Puntaje.Valor,
                aportes));
        }

        return resultado;
    }
}
