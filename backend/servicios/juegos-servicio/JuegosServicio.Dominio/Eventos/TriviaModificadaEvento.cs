namespace JuegosServicio.Dominio.Eventos;

public sealed class TriviaModificadaEvento : EventoDominio
{
    public Guid TriviaId { get; }
    public string NuevoNombre { get; }
    public int NuevoTiempoLimite { get; }

    public TriviaModificadaEvento(Guid triviaId, string nuevoNombre, int nuevoTiempoLimite)
    {
        TriviaId = triviaId;
        NuevoNombre = nuevoNombre;
        NuevoTiempoLimite = nuevoTiempoLimite;
    }
}
