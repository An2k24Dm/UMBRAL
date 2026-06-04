namespace JuegosServicio.Commons.Dtos;

// DTO de salida del detalle de misión expuesto al Participante a través
// del endpoint GET /api/juegos/misiones/participante/{misionId}.
//
// Es deliberadamente más pequeño que MisionDetalleDto: NO expone
// creadorId, fecha de creación ni acciones administrativas. El
// Participante solo necesita ver lo necesario para entender qué va a
// jugar (nombre, descripción, dificultad y etapas con tipo y duración).
public sealed class MisionDetalleParticipanteDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = default!;
    public string Descripcion { get; set; } = default!;
    public string Estado { get; set; } = default!;
    public string Dificultad { get; set; } = default!;
    public List<EtapaMisionParticipanteDto> Etapas { get; set; } = new();
}

public sealed class EtapaMisionParticipanteDto
{
    public Guid Id { get; set; }
    public int Orden { get; set; }
    public string TipoModoDeJuego { get; set; } = default!;
    public Guid ModoDeJuegoId { get; set; }
    public string NombreModoDeJuego { get; set; } = default!;
    public int TiempoEstimado { get; set; }
}
