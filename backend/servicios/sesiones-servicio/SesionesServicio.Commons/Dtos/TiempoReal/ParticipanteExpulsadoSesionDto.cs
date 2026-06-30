namespace SesionesServicio.Commons.Dtos.TiempoReal;

// HU44 — Evento dirigido al participante que fue expulsado de una sesión
// individual, para que su app reaccione (avisar y refrescar/navegar). No
// transporta datos sensibles.
public sealed class ParticipanteExpulsadoSesionDto
{
    public Guid SesionId { get; init; }
    public Guid ParticipanteSesionId { get; init; }
    public string TipoEvento { get; init; } = "ParticipanteExpulsadoSesion";
    public string Mensaje { get; init; } = "Fuiste expulsado de esta sesión.";
    public DateTime FechaEventoUtc { get; init; }
}
