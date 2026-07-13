namespace RankingServicio.Dominio.Excepciones;

// Excepción de dominio de ranking: se lanza cuando se intenta violar una
// invariante del agregado Ranking o del Value Object Puntaje (por ejemplo,
// un puntaje negativo o identificadores obligatorios ausentes).
public sealed class RankingInvalidoExcepcion : Exception
{
    public RankingInvalidoExcepcion(string mensaje) : base(mensaje) { }
}
