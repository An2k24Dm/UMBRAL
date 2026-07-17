namespace SesionesServicio.Aplicacion.Comandos.Penalizaciones;

// HU52 — Respuesta 202 Accepted al aplicar una penalización. El cálculo final
// es asíncrono (ranking-servicio); Sesiones solo confirma el registro y encolado.
public sealed record PenalizacionEncoladaDto(
    Guid PenalizacionId,
    Guid EventoId,
    string Estado);
