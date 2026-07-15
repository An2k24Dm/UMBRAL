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

    // Capacidad configurada de la sesión. Solo se llena la que aplica al modo:
    // MaximoParticipantes (Individual) o MaximoEquipos +
    // MaximoParticipantesPorEquipo (Grupal).
    public int? MaximoParticipantes { get; set; }
    public int? MaximoEquipos { get; set; }
    public int? MaximoParticipantesPorEquipo { get; set; }
    public int? DuracionSegundosLimite { get; set; }
    public EjecucionActualSesionDto? EjecucionActual { get; set; }

    public List<SesionMisionDto> Misiones { get; set; } = new();
    public List<EquipoSesionDto> Equipos { get; set; } = new();
    public List<ParticipanteSesionDto> ParticipantesIndividuales { get; set; } = new();
}

public sealed class EjecucionActualSesionDto
{
    public Guid MisionId { get; set; }
    public Guid EtapaId { get; set; }
    public Guid ModoDeJuegoId { get; set; }
    public string TipoEtapa { get; set; } = string.Empty;
    public int OrdenGlobal { get; set; }
    public DateTime FechaInicioUtc { get; set; }
    public int DuracionSegundos { get; set; }
    public long DuracionPausasAcumuladaMs { get; set; }
    public DateTime? FechaInicioPausaUtc { get; set; }
    public int SegundosRestantes { get; set; }
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
    public string Tipo { get; set; } = string.Empty;
    public int PuntajeActual { get; set; }
    public int CapacidadMaxima { get; set; }
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
    public Guid ParticipanteSesionId { get; set; }
    public Guid ParticipanteIdentidadId { get; set; }
    public string Alias { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public int Puntaje { get; set; }
    public DateTime FechaUnion { get; set; }
}
