namespace SesionesServicio.Commons.Dtos;

public sealed class SesionDetalleDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public string CodigoAcceso { get; set; } = string.Empty;
    public Guid OperadorCreadorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaInicioUtc { get; set; }
    public DateTime? FechaFinalizacionUtc { get; set; }
    public List<SesionMisionDto> Misiones { get; set; } = new();
    public List<EquipoSesionDto> Equipos { get; set; } = new();
    public List<ParticipanteSesionDto> ParticipantesIndividuales { get; set; } = new();
}

public sealed class SesionMisionDto
{
    public Guid Id { get; set; }
    public Guid MisionId { get; set; }
    public int Orden { get; set; }
}

public sealed class EquipoSesionDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int PuntajeActual { get; set; }
    public DateTime FechaCreacion { get; set; }
    public Guid LiderParticipanteId { get; set; }
    public List<ParticipanteEquipoDto> Participantes { get; set; } = new();
}

public sealed class ParticipanteEquipoDto
{
    public Guid Id { get; set; }
    public Guid ParticipanteId { get; set; }
    public DateTime FechaUnion { get; set; }
}

public sealed class ParticipanteSesionDto
{
    public Guid Id { get; set; }
    public Guid ParticipanteId { get; set; }
    public DateTime FechaUnion { get; set; }
}
