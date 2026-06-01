namespace SesionesServicio.Commons.Dtos;

// HU34 — Detalle completo de una sesión.
//
// El contenido asociado (Trivia o Búsqueda del Tesoro) se obtiene en
// línea desde juegos-servicio. Sólo una de las dos propiedades viene
// con valor según TipoJuego; la otra queda en null. No persistimos
// preguntas, opciones, etapas ni pistas aquí: juegos-servicio es el
// dueño de esa información.
//
// No incluye CreadaPorRol: el rol del creador se resuelve consultando
// a identidad-servicio cuando hace falta para la regla de visibilidad.
public sealed class SesionDetalleDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string TipoJuego { get; set; } = string.Empty;
    public Guid ContenidoJuegoId { get; set; }
    public string Modo { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaProgramada { get; set; }
    public Guid CreadaPorUsuarioId { get; set; }
    public DateTime FechaCreacion { get; set; }

    public DetalleTriviaSesionDto? Trivia { get; set; }
    public DetalleBusquedaSesionDto? BusquedaTesoro { get; set; }
}
