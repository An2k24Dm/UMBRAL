using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;

namespace JuegosServicio.Aplicacion.Puertos;

public interface IRepositorioBusquedas
{
    Task<bool> ExisteBusquedaConNombreAsync(string nombre, CancellationToken cancelacion);
    Task CrearBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion);
    Task<BusquedaTesoro?> ObtenerBusquedaPorIdAsync(Guid busquedaId, CancellationToken cancelacion);
    Task<List<BusquedaTesoroResumenDto>> ObtenerBusquedasEnBorradorAsync(Guid? creadorId, CancellationToken cancelacion);
    Task AgregarEtapaAsync(Guid busquedaId, Etapa etapa, CancellationToken cancelacion);
    Task ModificarEtapaAsync(Guid busquedaId, Etapa etapa, CancellationToken cancelacion);
    Task EliminarEtapaAsync(Guid busquedaId, Guid etapaId, CancellationToken cancelacion);
    Task AgregarMisionAsync(Guid etapaId, Mision mision, CancellationToken cancelacion);
    Task ModificarMisionAsync(Guid etapaId, Mision mision, CancellationToken cancelacion);
    Task EliminarMisionAsync(Guid etapaId, Guid misionId, CancellationToken cancelacion);
    Task ActivarBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion);
    Task ArchivarBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion);
    Task<List<BusquedaTesoroResumenDto>> ObtenerBusquedasActivasAsync(CancellationToken cancelacion);
    Task<BusquedaTesoroDetalleDto?> ObtenerDetalleBusquedaAsync(Guid busquedaId, CancellationToken cancelacion);
    Task AgregarPistaAsync(Guid etapaId, Pista pista, CancellationToken cancelacion);
}
