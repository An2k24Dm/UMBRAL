using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Infraestructura.Persistencia;

public sealed class SesionModelo
{
    public Guid Id { get; set; }
    public string TipoSesion { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public EstadoSesion Estado { get; set; }
    public DateTime FechaProgramada { get; set; }
    public string CodigoAcceso { get; set; } = string.Empty;
    public Guid OperadorCreadorId { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaInicioUtc { get; set; }
    public DateTime? FechaFinalizacionUtc { get; set; }
    public int? MaximoParticipantes { get; set; }
    public int? MaximoEquipos { get; set; }
    public int? MaximoParticipantesPorEquipo { get; set; }
    public int? DuracionSegundosLimite { get; set; }
    public Guid? EjecucionActualMisionId { get; set; }
    public Guid? EjecucionActualEtapaId { get; set; }
    public Guid? EjecucionActualModoDeJuegoId { get; set; }
    public string? EjecucionActualTipoEtapa { get; set; }
    public int? EjecucionActualOrdenGlobal { get; set; }
    public int? EjecucionActualOrdenMision { get; set; }
    public int? EjecucionActualOrdenEtapa { get; set; }
    public int? EjecucionActualFase { get; set; }
    public int? EjecucionActualDuracionPreparacionSegundos { get; set; }
    public DateTime? EjecucionActualFechaInicioUtc { get; set; }
    public int? EjecucionActualDuracionSegundos { get; set; }
    public long? EjecucionActualDuracionPausasAcumuladaMs { get; set; }
    public DateTime? EjecucionActualFechaInicioPausaUtc { get; set; }
    // Plan global de etapas de la sesión serializado como JSON (ver
    // EtapaPlanificadaSesion). Null hasta que la sesión inicia.
    public string? SecuenciaEtapasJson { get; set; }
    public List<SesionMisionModelo> Misiones { get; set; } = new();
    public List<EquipoModelo> Equipos { get; set; } = new();
    public List<ParticipanteModelo> Participantes { get; set; } = new();
}

public sealed class SesionMisionModelo
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public Guid MisionId { get; set; }
    public int Orden { get; set; }
}

public sealed class EquipoModelo
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid LiderParticipanteId { get; set; }
    public int Puntaje { get; set; }
    public TipoEquipo Tipo { get; set; }
    // Hash de la contraseña (solo equipos privados); null en públicos.
    public string? ContrasenaHash { get; set; }
    public int CapacidadMaxima { get; set; }
    public DateTime FechaCreacion { get; set; }
}

public sealed class ParticipanteModelo
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public Guid ParticipanteIdentidadId { get; set; }
    public Guid? EquipoId { get; set; }
    public int Puntaje { get; set; }
    public DateTime FechaUnionSesion { get; set; }
    public DateTime? FechaUnionEquipo { get; set; }
}

public sealed class EtapaCompletadaModelo
{
    public Guid SesionId { get; set; }
    public Guid EtapaId { get; set; }
    public DateTime FechaCompletadaUtc { get; set; }
}

public sealed class RespuestaTriviaModelo
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public Guid MisionId { get; set; }
    public Guid EtapaId { get; set; }
    public Guid TriviaId { get; set; }
    public Guid PreguntaId { get; set; }
    public Guid? OpcionSeleccionadaId { get; set; }
    public Guid ParticipanteIdentidadId { get; set; }
    public Guid? EquipoId { get; set; }
    public bool EsCorrecta { get; set; }
    public int PuntosGanados { get; set; }
    public int TiempoTardadoMs { get; set; }
    public DateTime FechaRespuestaUtc { get; set; }
}
