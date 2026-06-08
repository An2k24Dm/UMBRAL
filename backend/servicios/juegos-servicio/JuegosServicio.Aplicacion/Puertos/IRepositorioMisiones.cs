using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;

namespace JuegosServicio.Aplicacion.Puertos;

public interface IRepositorioMisiones
{
    Task<bool> ExisteMisionConNombreAsync(string nombre, CancellationToken cancelacion);
    Task<bool> EsContenidoUsadoEnEtapaAsync(TipoModoDeJuego tipo, Guid contenidoId, CancellationToken cancelacion);
    Task<bool> EsContenidoUsadoEnMisionActivaAsync(TipoModoDeJuego tipo, Guid contenidoId, CancellationToken cancelacion);
    Task CrearMisionAsync(Mision mision, CancellationToken cancelacion);
    Task<Mision?> ObtenerMisionPorIdAsync(Guid misionId, CancellationToken cancelacion);
    Task<List<MisionResumenDto>> ObtenerMisionesEnBorradorAsync(Guid? creadorId, CancellationToken cancelacion);
    Task<List<MisionResumenDto>> ObtenerMisionesActivasAsync(CancellationToken cancelacion);
    Task<MisionDetalleDto?> ObtenerDetalleMisionAsync(Guid misionId, CancellationToken cancelacion);
    Task AgregarEtapaAsync(Etapa etapa, CancellationToken cancelacion);
    Task EliminarEtapaAsync(Guid etapaId, CancellationToken cancelacion);
    Task ActualizarOrdenesEtapasAsync(IEnumerable<Etapa> etapas, CancellationToken cancelacion);
    Task ActualizarMisionAsync(Mision mision, CancellationToken cancelacion);
    Task ActivarMisionAsync(Mision mision, CancellationToken cancelacion);
    Task DesactivarMisionAsync(Mision mision, CancellationToken cancelacion);
    Task EliminarMisionAsync(Guid misionId, CancellationToken cancelacion);
}
