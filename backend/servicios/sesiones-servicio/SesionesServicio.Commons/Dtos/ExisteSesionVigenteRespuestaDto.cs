namespace SesionesServicio.Commons.Dtos;

// HU33 — Respuesta del endpoint que informa a juegos-servicio si un
// contenido de juego (Trivia o Búsqueda del Tesoro) tiene al menos una
// sesión en estado vigente (Programada, EnPreparacion, Activa o
// Pausada). Juegos-servicio usa este booleano para bloquear la
// desactivación del contenido y evitar dejar sesiones huérfanas.
public sealed class ExisteSesionVigenteRespuestaDto
{
    public bool Existe { get; set; }
}
