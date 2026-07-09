namespace SesionesServicio.Commons.Dtos;

public sealed class ProgresoTriviaParticipanteDto
{
    public Guid ParticipanteIdentidadId { get; set; }
    public int TotalRespondidas { get; set; }
    public int Correctas { get; set; }
    public int Incorrectas { get; set; }
    public int PuntosGanados { get; set; }
}
