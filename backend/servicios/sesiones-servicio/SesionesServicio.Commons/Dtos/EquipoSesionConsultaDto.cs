namespace SesionesServicio.Commons.Dtos;

// HU43 — Equipo en el listado de equipos de una sesión. Nunca incluye
// contraseña ni hash.
public sealed class EquipoSesionListadoDto
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Puntaje { get; set; }
    public int CantidadParticipantes { get; set; }
    public int CapacidadMaxima { get; set; }
    public bool EstaLleno { get; set; }
    public DateTime FechaCreacion { get; set; }
    public bool EsMiEquipo { get; set; }
    public bool SoyLider { get; set; }
}

// HU43 — Detalle de un equipo con sus integrantes.
public sealed class EquipoSesionDetalleDto
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int Puntaje { get; set; }
    public int CantidadParticipantes { get; set; }
    public int CapacidadMaxima { get; set; }
    public DateTime FechaCreacion { get; set; }
    public bool EstaLleno { get; set; }
    public Guid LiderParticipanteId { get; set; }
    public bool EsMiEquipo { get; set; }
    public bool SoyLider { get; set; }
    public List<IntegranteEquipoDto> Participantes { get; set; } = new();
}

// HU43 — Integrante de un equipo. Solo datos no sensibles.
public sealed class IntegranteEquipoDto
{
    public Guid ParticipanteSesionId { get; set; }
    public Guid ParticipanteIdentidadId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public int Puntaje { get; set; }
    public DateTime FechaUnion { get; set; }
    public bool EsLider { get; set; }
}
