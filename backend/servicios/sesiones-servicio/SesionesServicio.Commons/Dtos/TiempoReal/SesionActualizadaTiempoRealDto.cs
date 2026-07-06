namespace SesionesServicio.Commons.Dtos.TiempoReal;

// Evento SignalR "SesionActualizada": avisa a los clientes del grupo de la
// sesión y del listado que el estado del ciclo de vida cambió. SignalR solo
// notifica; el cliente vuelve a consultar por HTTP la fuente de verdad.
public sealed class SesionActualizadaTiempoRealDto
{
    public Guid SesionId { get; init; }
    public string Estado { get; init; } = string.Empty;
    public string TipoEvento { get; init; } = "SesionActualizada";
    public DateTime FechaEventoUtc { get; init; }
}
