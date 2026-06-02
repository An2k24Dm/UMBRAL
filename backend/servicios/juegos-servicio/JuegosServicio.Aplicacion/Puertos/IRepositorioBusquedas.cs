using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;

namespace JuegosServicio.Aplicacion.Puertos;

public interface IRepositorioBusquedas
{
    Task<bool> ExisteBusquedaConNombreAsync(string nombre, CancellationToken cancelacion);
    Task CrearBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion);
    Task<BusquedaTesoro?> ObtenerBusquedaPorIdAsync(Guid busquedaId, CancellationToken cancelacion);
    Task<List<BusquedaTesoroResumenDto>> ObtenerBusquedasEnBorradorAsync(Guid? creadorId, CancellationToken cancelacion);
    Task<List<BusquedaTesoroResumenDto>> ObtenerBusquedasActivasAsync(CancellationToken cancelacion);
    Task<BusquedaTesoroDetalleDto?> ObtenerDetalleBusquedaAsync(Guid busquedaId, CancellationToken cancelacion);
    Task AsignarMisionAsync(Guid busquedaId, Mision mision, CancellationToken cancelacion);
    Task ModificarMisionAsync(Mision mision, CancellationToken cancelacion);
    Task EliminarMisionAsync(Guid busquedaId, CancellationToken cancelacion);
    Task AgregarPistaAsync(Guid misionId, Pista pista, CancellationToken cancelacion);
    Task ModificarPistaAsync(Pista pista, CancellationToken cancelacion);
    Task EliminarPistaAsync(Guid pistaId, CancellationToken cancelacion);
    Task ActivarBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion);
    Task DesactivarBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion);
    Task EliminarBusquedaTesoroAsync(Guid busquedaId, CancellationToken cancelacion);
}
