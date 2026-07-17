using RankingServicio.Dominio.Excepciones;

namespace RankingServicio.Dominio.ObjetosValor;

// Value Object inmutable que encapsula el puntaje ACUMULADO de ranking. Es un
// entero; cero es el puntaje inicial válido. A partir de HU52 el acumulado
// PUEDE quedar negativo (una penalización mayor al puntaje ganado no se limita
// a cero). Igualdad por valor. Se distinguen tres conceptos:
//   - puntaje ganado: nunca negativo (Desde / Sumar rechazan negativos);
//   - puntaje acumulado: puede ser negativo (DesdePersistencia lo admite);
//   - penalización: se aplica como delta negativo (AplicarPenalizacion).
// Es un tipo independiente de ranking-servicio: no se comparte dominio entre
// microservicios.
public sealed class Puntaje : IEquatable<Puntaje>
{
    public long Valor { get; }

    private Puntaje(long valor) => Valor = valor;

    public static Puntaje Cero => new(0);

    // Puntaje GANADO: las estrategias de puntaje solo generan valores >= 0. Se
    // mantiene la invariante para que Trivia/Tesoro nunca envíen ganados
    // negativos.
    public static Puntaje Desde(long valor)
    {
        if (valor < 0)
            throw new RankingInvalidoExcepcion("El puntaje ganado no puede ser negativo.");
        return new Puntaje(valor);
    }

    // Puntaje ACUMULADO desde persistencia o recálculo: admite negativos porque
    // tras aplicar penalizaciones el acumulado puede ser < 0.
    public static Puntaje DesdePersistencia(long valor) => new(valor);

    // Suma una variación de puntos GANADOS. El delta sumado nunca es negativo
    // (solo se usa para acumular puntaje ganado); el acumulado resultante sí
    // puede ser negativo si el puntaje previo ya lo era.
    public Puntaje Sumar(long puntos)
    {
        if (puntos < 0)
            throw new RankingInvalidoExcepcion(
                "El puntaje a sumar no puede ser negativo.");
        return new Puntaje(Valor + puntos);
    }

    // HU52 — Aplica una penalización como variación NEGATIVA. El resultado puede
    // quedar negativo: no se limita a cero ni se rechaza si supera el acumulado.
    public Puntaje AplicarPenalizacion(CantidadPenalizacion penalizacion)
    {
        if (penalizacion is null)
            throw new RankingInvalidoExcepcion(
                "La cantidad de penalización es obligatoria.");
        return new Puntaje(Valor - penalizacion.Valor);
    }

    public bool Equals(Puntaje? otro) => otro is not null && Valor == otro.Valor;

    public override bool Equals(object? obj) => Equals(obj as Puntaje);

    public override int GetHashCode() => Valor.GetHashCode();

    public override string ToString() => Valor.ToString();
}
