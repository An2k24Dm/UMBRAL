namespace JuegosServicio.Dominio.Eventos;

public sealed class TriviaActivadaEvento : EventoDominio
{
    public Guid TriviaId { get; }
    public string Nombre { get; }
    public int CantidadPreguntas { get; }

    public TriviaActivadaEvento(Guid triviaId, string nombre, int cantidadPreguntas)
    {
        TriviaId = triviaId;
        Nombre = nombre;
        CantidadPreguntas = cantidadPreguntas;
    }
}
