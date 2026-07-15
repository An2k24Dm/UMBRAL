using SesionesServicio.Commons.Dtos;
namespace SesionesServicio.Aplicacion.Puertos;

public interface IClienteJuegosMisiones
{
    Task<MisionResumenJuegosDto?> ObtenerMisionAsync(
        Guid misionId, CancellationToken cancelacion);
    Task<MisionConEtapasJuegosDto?> ObtenerMisionConEtapasAsync(
        Guid misionId, CancellationToken cancelacion);
}
