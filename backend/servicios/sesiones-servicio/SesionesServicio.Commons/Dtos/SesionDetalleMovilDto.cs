namespace SesionesServicio.Commons.Dtos;

public sealed class SesionDetalleMovilDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Modo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public string CodigoAcceso { get; set; } = string.Empty;
    public List<MisionSesionMovilDto> Misiones { get; set; } = new();

    // Estado de participación del usuario autenticado en esta sesión. Permite
    // al móvil decidir si ofrecer "Unirse" o mostrar que ya pertenece.
    public ParticipacionActualDto ParticipacionActual { get; set; } = new();
}

// Resumen mínimo de la participación del usuario actual (HU40). No expone
// listados ni detalle del equipo: eso corresponde a HU43.
public sealed class ParticipacionActualDto
{
    public bool EstaInscrito { get; set; }
    // "Individual" | "Equipo" | null (cuando no está inscrito).
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
    public string NombreModoDeJuego { get; set; } = string.Empty;
    public int? TiempoEstimadoSegundos { get; set; }
}
