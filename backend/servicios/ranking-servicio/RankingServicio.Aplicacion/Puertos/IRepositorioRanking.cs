using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Puertos;

// Repositorio del Aggregate Root Ranking. Se usa para operaciones de
// dominio/escritura (procesar eventos). Las consultas globales optimizadas
// usan IConsultasRanking (proyección CQRS).
public interface IRepositorioRanking
{
    Task<Ranking?> ObtenerPorSesionAsync(Guid sesionId, CancellationToken cancelacion);
    Task AgregarAsync(Ranking ranking, CancellationToken cancelacion);
    Task ActualizarAsync(Ranking ranking, CancellationToken cancelacion);
}
