using PartidasServicio.Commons.Dtos;

namespace PartidasServicio.Aplicacion.Puertos;

public interface INotificadorPartidasTiempoReal
{
    Task NotificarPuntajeActualizadoAsync(
        Guid sesionId,
        IReadOnlyList<RankingEntradaDto> ranking,
        CancellationToken cancelacion);
}
