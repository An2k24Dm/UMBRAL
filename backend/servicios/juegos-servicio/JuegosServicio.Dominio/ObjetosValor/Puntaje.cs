using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.ObjetosValor;

// Objeto de valor inmutable para los puntajes del catálogo de juegos.
// Cada fábrica aplica las reglas de su contexto; la igualdad es por valor.
public sealed class Puntaje : IEquatable<Puntaje>
{
    public const int MinimoPregunta = 5;
    public const int MaximoPregunta = 100;
    public const int MultiploPregunta = 5;
    public const int MinimoBusqueda = 5;

    public static Puntaje Cero { get; } = new(0);

    public int Valor { get; }

    private Puntaje(int valor) => Valor = valor;

    public static Puntaje CrearParaPregunta(int valor)
    {
        if (valor <= 0 || valor % MultiploPregunta != 0)
            throw new ExcepcionDominio("El puntaje debe ser un múltiplo de 5 (5, 10, 15… 100).");
        if (valor > MaximoPregunta)
            throw new ExcepcionDominio("El puntaje máximo por pregunta es 100.");
        return new Puntaje(valor);
    }

    public static Puntaje CrearParaBusqueda(int valor)
    {
        if (valor < 0)
            throw new ExcepcionDominio("El puntaje no puede ser negativo.");
        if (valor < MinimoBusqueda)
            throw new ExcepcionDominio($"El puntaje debe ser al menos {MinimoBusqueda} puntos.");
        return new Puntaje(valor);
    }

    // Rehidratación desde la base de datos: acepta datos existentes
    // (incluido 0 legacy o default de BD), pero nunca negativos.
    public static Puntaje DesdePersistencia(int valor)
    {
        if (valor < 0)
            throw new ExcepcionDominio("El puntaje no puede ser negativo.");
        return new Puntaje(valor);
    }

    public bool Equals(Puntaje? otro) => otro is not null && Valor == otro.Valor;

    public override bool Equals(object? obj) => Equals(obj as Puntaje);

    public override int GetHashCode() => Valor.GetHashCode();

    public static bool operator ==(Puntaje? izquierda, Puntaje? derecha) =>
        izquierda is null ? derecha is null : izquierda.Equals(derecha);

    public static bool operator !=(Puntaje? izquierda, Puntaje? derecha) =>
        !(izquierda == derecha);

    public override string ToString() => Valor.ToString();
}
