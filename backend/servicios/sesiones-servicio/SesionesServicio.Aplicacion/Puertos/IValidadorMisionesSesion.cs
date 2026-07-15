using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IValidadorMisionesSesion
{
    Task<ResultadoValidacionMisionesSesion> ValidarYObtenerAsync(
        IReadOnlyList<Guid> misionesIds, CancellationToken cancelacion);
}

public sealed record ResultadoValidacionMisionesSesion(
    IReadOnlyList<MisionResumenJuegosDto> Misiones,
    int DuracionTotalSegundos);
