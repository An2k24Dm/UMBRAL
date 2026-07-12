using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Puertos;

public interface IRepositorioRankingEquipo
{
    Task<EntradaRankingEquipo?> ObtenerPorSesionYEquipoAsync(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion);
    Task<List<EntradaRankingEquipo>> ObtenerPorSesionAsync(
        Guid sesionId, CancellationToken cancelacion);
    Task AgregarAsync(EntradaRankingEquipo entrada, CancellationToken cancelacion);
    Task ActualizarAsync(EntradaRankingEquipo entrada, CancellationToken cancelacion);
}
