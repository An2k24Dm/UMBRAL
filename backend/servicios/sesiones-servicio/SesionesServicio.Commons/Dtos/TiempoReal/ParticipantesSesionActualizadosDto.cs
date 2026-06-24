namespace SesionesServicio.Commons.Dtos.TiempoReal;

public sealed class ParticipantesSesionActualizadosDto
{
    public Guid SesionId { get; init; }
    public string TipoEvento { get; init; } = "ParticipantesSesionActualizados";
    public DateTime FechaEventoUtc { get; init; }
}
