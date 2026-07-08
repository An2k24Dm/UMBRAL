using SesionesServicio.Commons.Dtos;

namespace SesionesServicio.Aplicacion.Puertos;

// Patrón Facade: puerto único de coordinación de las operaciones del ciclo de
// vida de una sesión desde el panel del Operador. La implementación
// (FachadaOperacionSesion, en Aplicacion/Fachadas) encapsula autorización,
// carga, validadores de reglas extra, dominio (State pattern), persistencia,
// notificación en tiempo real y logging de aplicación. Los manejadores CQRS
// solo delegan aquí.
public interface IFachadaOperacionSesion
{
    Task<OperacionSesionRespuestaDto> IniciarAsync(Guid sesionId, CancellationToken cancelacion);
    Task<OperacionSesionRespuestaDto> PausarAsync(Guid sesionId, CancellationToken cancelacion);
    Task<OperacionSesionRespuestaDto> ReanudarAsync(Guid sesionId, CancellationToken cancelacion);
    Task<OperacionSesionRespuestaDto> CancelarAsync(Guid sesionId, CancellationToken cancelacion);
    Task<OperacionSesionRespuestaDto?> FinalizarSiCorrespondeAsync(
        Guid sesionId, CancellationToken cancelacion);
}
