using Microsoft.EntityFrameworkCore;
using RankingServicio.Aplicacion.Puertos;

namespace RankingServicio.Infraestructura.Persistencia.Consultas;

// Proyección CQRS optimizada. El ranking global se calcula en la base de datos
// (GROUP BY + SUM) sobre los RankingParticipante de todas las sesiones, sin
// materializar agregados en memoria y sin ninguna entidad global persistida.
public sealed class ConsultasRanking : IConsultasRanking
{
    private readonly ContextoRanking _contexto;

    public ConsultasRanking(ContextoRanking contexto)
    {
        _contexto = contexto;
    }

    public async Task<IReadOnlyList<RankingGlobalProyeccion>> ObtenerRankingGlobalAsync(
        int top, CancellationToken cancelacion)
    {
        var limite = top <= 0 ? 20 : top;

        var filas = await _contexto.Database
            .SqlQuery<RankingGlobalProyeccion>(
                $"""
                 SELECT participante_identidad_id AS "ParticipanteIdentidadId",
                        SUM(puntaje) AS "Puntaje"
                 FROM ranking.ranking_participantes
                 GROUP BY participante_identidad_id
                 ORDER BY SUM(puntaje) DESC
                 LIMIT {limite}
                 """)
            .ToListAsync(cancelacion);

        return filas;
    }
}
