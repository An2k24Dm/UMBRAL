namespace SesionesServicio.Commons.Dtos;

public sealed class SesionDetalleMovilDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public DateTime? FechaInicioUtc { get; set; }
    public int? DuracionMinutosLimite { get; set; }
    public string CodigoAcceso { get; set; } = string.Empty;
    public List<MisionSesionMovilDto> Misiones { get; set; } = new();
    public ParticipacionActualDto ParticipacionActual { get; set; } = new();
    public bool PuedeIngresar { get; set; } = true;
    public string? MotivoNoPuedeIngresar { get; set; }
    public Guid? SesionActualId { get; set; }
    public string? SesionActualNombre { get; set; }
}

public sealed class ParticipacionActualDto
{
    public bool EstaInscrito { get; set; }
    public string? Tipo { get; set; }
    public Guid? EquipoId { get; set; }
    public string? EquipoNombre { get; set; }
    public bool EsLider { get; set; }
    public Guid? ParticipanteSesionId { get; set; }
}

public sealed class MisionSesionMovilDto
{
    public Guid Id { get; set; }
    public int Orden { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string? Dificultad { get; set; }
    public int TotalEtapas { get; set; }
    public List<EtapaSesionMovilDto> Etapas { get; set; } = new();
}

public sealed class EtapaSesionMovilDto
{
    public Guid Id { get; set; }
    public int Orden { get; set; }
    public string TipoModoDeJuego { get; set; } = string.Empty;
    public Guid ModoDeJuegoId { get; set; }
    public string NombreModoDeJuego { get; set; } = string.Empty;
    public int? TiempoEstimadoSegundos { get; set; }
}
