using MediatR;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.Aplicacion.Consultas.ObtenerRankingSesion;

public sealed class ObtenerRankingSesionManejador
    : IRequestHandler<ObtenerRankingSesionConsulta, IReadOnlyList<RankingEntradaDto>>
{
    private readonly IConsultasPartidas _consultas;
    private readonly IClienteSesiones _clienteSesiones;

    public ObtenerRankingSesionManejador(IConsultasPartidas consultas, IClienteSesiones clienteSesiones)
    {
        _consultas = consultas;
        _clienteSesiones = clienteSesiones;
    }

    public async Task<IReadOnlyList<RankingEntradaDto>> Handle(
        ObtenerRankingSesionConsulta consulta, CancellationToken cancelacion)
    {
        var ranking = await _consultas.ObtenerRankingAsync(consulta.SesionId, cancelacion);

        try
        {
            var nombres = await _clienteSesiones.ObtenerNombresRankingAsync(consulta.SesionId, cancelacion);
            if (nombres is not null)
                EnriquecerNombres(ranking, nombres);
        }
        catch
        {
            // Enrichment failure is non-fatal; names remain empty strings.
        }

        return ranking;
    }

    private static void EnriquecerNombres(
        IReadOnlyList<RankingEntradaDto> ranking,
        NombresRankingClienteDto nombres)
    {
        var equiposPorId = nombres.Equipos.ToDictionary(e => e.Id, e => e.Nombre);
        var participantesPorId = nombres.Participantes.ToDictionary(p => p.IdentidadId, p => p.Alias);
        foreach (var entrada in ranking)
        {
            if (entrada.EquipoId is Guid eqId && equiposPorId.TryGetValue(eqId, out var nombre))
                entrada.Nombre = nombre;
            else if (entrada.ParticipanteId is Guid partId && participantesPorId.TryGetValue(partId, out var alias))
                entrada.Nombre = alias;
        }
    }
}
