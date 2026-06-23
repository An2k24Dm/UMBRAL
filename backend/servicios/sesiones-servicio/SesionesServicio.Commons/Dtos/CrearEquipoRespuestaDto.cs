namespace SesionesServicio.Commons.Dtos;

// Respuesta tras crear un equipo. Nunca incluye la contraseña ni su hash.
public sealed class CrearEquipoRespuestaDto
{
    public Guid Id { get; set; }
    public Guid SesionId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int CapacidadMaxima { get; set; }
    public int CantidadParticipantes { get; set; }
    public Guid LiderParticipanteId { get; set; }
    public int Puntaje { get; set; }
    public DateTime FechaCreacion { get; set; }
}
