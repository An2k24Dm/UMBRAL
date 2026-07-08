using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IClienteBusquedaTesoro
{
    Task<BusquedaTesoroJuegosDto?> ObtenerBusquedaParticipanteAsync(
        Guid busquedaId, CancellationToken cancelacion);

    Task<bool?> ValidarCodigoQrAsync(
        Guid busquedaId, string codigoEscaneado, CancellationToken cancelacion);
}
