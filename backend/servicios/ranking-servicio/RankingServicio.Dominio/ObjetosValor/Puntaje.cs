using RankingServicio.Dominio.Excepciones;

namespace RankingServicio.Dominio.ObjetosValor;

// Value Object inmutable que encapsula el puntaje de ranking. Es un entero
// nunca negativo; cero es el puntaje inicial válido. Igualdad por valor.
// Replicando la invariante vigente del sistema (SesionesServicio.PuntajeSesion
// también es no negativo), pero es un tipo independiente de ranking-servicio:
// no se comparte ni reutiliza dominio entre microservicios.
public sealed class Puntaje : IEquatable<Puntaje>
{
    public long Valor { get; }

    private Puntaje(long valor) => Valor = valor;

    public static Puntaje Cero => new(0);

    public static Puntaje Desde(long valor)
    {
        if (valor < 0)
            throw new RankingInvalidoExcepcion("El puntaje no puede ser negativo.");
        return new Puntaje(valor);
    }

    // Suma una variación de puntos. El sistema solo genera puntajes ganados
    // (>= 0); no se permiten deltas negativos, igual que PuntajeSesion.Sumar.
    public Puntaje Sumar(long puntos)
    {
        if (puntos < 0)
            throw new RankingInvalidoExcepcion(
                "El puntaje a sumar no puede ser negativo.");
        return new Puntaje(Valor + puntos);
    }

    public bool Equals(Puntaje? otro) => otro is not null && Valor == otro.Valor;

    public override bool Equals(object? obj) => Equals(obj as Puntaje);

    public override int GetHashCode() => Valor.GetHashCode();

    public override string ToString() => Valor.ToString();
}
