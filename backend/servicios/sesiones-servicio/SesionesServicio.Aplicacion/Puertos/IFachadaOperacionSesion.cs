using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IFachadaOperacionSesion
{
    Task<OperacionSesionRespuestaDto> IniciarAsync(Guid sesionId, CancellationToken cancelacion);
    Task<OperacionSesionRespuestaDto> PausarAsync(Guid sesionId, CancellationToken cancelacion);
    Task<OperacionSesionRespuestaDto> ReanudarAsync(Guid sesionId, CancellationToken cancelacion);
    Task<OperacionSesionRespuestaDto> CancelarAsync(Guid sesionId, CancellationToken cancelacion);
    Task<OperacionSesionRespuestaDto?> FinalizarSiCorrespondeAsync(
        Guid sesionId, CancellationToken cancelacion);
}
