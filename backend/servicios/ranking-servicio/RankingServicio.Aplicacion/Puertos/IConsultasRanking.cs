namespace RankingServicio.Aplicacion.Puertos;

// Puerto de consulta CQRS para proyecciones eficientes sobre la persistencia,
// sin materializar agregados completos en memoria. El ranking global se calcula
// aquí como GROUP BY ParticipanteIdentidadId + SUM(Puntaje) sobre los
// RankingParticipante de todas las sesiones; no existe ninguna entidad global
// persistida (no hay segunda fuente de verdad).
public interface IConsultasRanking
{
    Task<IReadOnlyList<RankingGlobalProyeccion>> ObtenerRankingGlobalAsync(
        int top, CancellationToken cancelacion);
}

// Proyección: puntaje total acumulado por identidad de participante.
public sealed record RankingGlobalProyeccion(
    Guid ParticipanteIdentidadId,
    long Puntaje);
