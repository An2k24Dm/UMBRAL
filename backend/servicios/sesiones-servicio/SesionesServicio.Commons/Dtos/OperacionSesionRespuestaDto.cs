namespace SesionesServicio.Commons.Dtos;

public sealed class OperacionSesionRespuestaDto
{
    public Guid SesionId { get; init; }
    public string Estado { get; init; } = string.Empty;
    public DateTime? FechaInicioUtc { get; init; }
    public DateTime? FechaFinalizacionUtc { get; init; }
    public string Mensaje { get; init; } = string.Empty;
}
