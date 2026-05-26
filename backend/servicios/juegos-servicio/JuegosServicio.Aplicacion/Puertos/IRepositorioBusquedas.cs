using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;

namespace JuegosServicio.Aplicacion.Puertos;

public interface IRepositorioBusquedas
{
    Task<bool> ExisteBusquedaConNombreAsync(string nombre, CancellationToken cancelacion);
    Task CrearBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion);
    Task<BusquedaTesoro?> ObtenerBusquedaPorIdAsync(Guid busquedaId, CancellationToken cancelacion);
    Task<List<BusquedaTesoroResumenDto>> ObtenerBusquedasEnBorradorAsync(Guid creadorId, CancellationToken cancelacion);
}
