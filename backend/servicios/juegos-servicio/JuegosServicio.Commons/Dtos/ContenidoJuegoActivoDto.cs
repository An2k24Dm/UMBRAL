namespace JuegosServicio.Commons.Dtos;

// HU33 — Resumen del contenido (Trivia o BúsquedaTesoro) usado por
// sesiones-servicio para validar que el contenido existe y está Activo
// antes de permitir crear una sesión en vivo.
public sealed class ContenidoJuegoActivoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoJuego { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public bool EstaActivo { get; set; }
}
