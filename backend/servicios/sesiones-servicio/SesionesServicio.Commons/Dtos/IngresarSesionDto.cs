namespace SesionesServicio.Commons.Dtos;

public sealed class IngresarSesionDto
{
    public string CodigoSesion { get; init; } = string.Empty;
}

public sealed class IngresarSesionRespuestaDto
{
    public Guid SesionId { get; init; }
    public string NombreSesion { get; init; } = string.Empty;
    public string CodigoSesion { get; init; } = string.Empty;
    public string Estado { get; init; } = string.Empty;
    public string Modo { get; init; } = string.Empty;
    public bool IngresoRegistrado { get; init; }
    public bool RedirigirADetalle { get; init; }
    public bool RequiereEquipo { get; init; }
    public bool PuedeCrearEquipo { get; init; }
    public bool YaPertenecia { get; init; }
    public string? Mensaje { get; init; }
    public ParticipacionActualDto? ParticipacionActual { get; init; }
    public IReadOnlyCollection<ContenidoSesionMovilDto> Contenido { get; init; } =
        Array.Empty<ContenidoSesionMovilDto>();
}

public sealed class ContenidoSesionMovilDto
{
    public Guid MisionId { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Tipo { get; init; } = string.Empty;
    public int Orden { get; init; }
    public string? Descripcion { get; init; }
    public int? TiempoLimite { get; init; }
}
