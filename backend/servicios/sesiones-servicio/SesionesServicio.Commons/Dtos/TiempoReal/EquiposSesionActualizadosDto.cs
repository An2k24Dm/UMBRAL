namespace SesionesServicio.Commons.Dtos.TiempoReal;

public sealed class EquiposSesionActualizadosDto
{
    public Guid SesionId { get; init; }
    public Guid? EquipoId { get; init; }
    public string TipoEvento { get; init; } = "EquiposSesionActualizados";
    public DateTime FechaEventoUtc { get; init; }
}
