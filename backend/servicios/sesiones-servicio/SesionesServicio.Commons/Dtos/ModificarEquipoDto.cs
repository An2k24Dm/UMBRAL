namespace SesionesServicio.Commons.Dtos;

public sealed class ModificarEquipoDto
{
    public string Nombre { get; set; } = string.Empty;
    public TipoEquipoDto Tipo { get; set; }
    public string? Contrasena { get; set; }
}

public sealed class ModificarEquipoRespuestaDto
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int CapacidadMaxima { get; set; }
    public int CantidadParticipantes { get; set; }
    public Guid LiderParticipanteId { get; set; }
    public int Puntaje { get; set; }
    public DateTime FechaCreacion { get; set; }
}
