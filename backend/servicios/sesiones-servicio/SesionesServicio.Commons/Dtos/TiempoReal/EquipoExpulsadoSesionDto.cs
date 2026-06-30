namespace SesionesServicio.Commons.Dtos.TiempoReal;

// HU44 — Evento dirigido a cada integrante de un equipo expulsado de una
// sesión grupal, para que su app reaccione (avisar y refrescar/navegar). No
// transporta datos sensibles.
public sealed class EquipoExpulsadoSesionDto
{
    public Guid SesionId { get; init; }
    public Guid EquipoId { get; init; }
    public string EquipoNombre { get; init; } = string.Empty;
    public string TipoEvento { get; init; } = "EquipoExpulsadoSesion";
    public string Mensaje { get; init; } = "Tu equipo fue expulsado de la sesión.";
    public DateTime FechaEventoUtc { get; init; }
}
