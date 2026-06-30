namespace SesionesServicio.Commons.Dtos.TiempoReal;

public sealed class EquipoActualizadoTiempoRealDto
{
    public Guid SesionId { get; init; }
    public Guid EquipoId { get; init; }
    public string TipoEvento { get; init; } = "EquipoActualizado";
    public DateTime FechaEventoUtc { get; init; }
}
