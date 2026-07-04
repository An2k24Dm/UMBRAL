using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.ObjetosValor;

// Objeto de valor inmutable para las duraciones del catálogo de juegos.
// La unidad depende del contexto: segundos en trivias y minutos en
// búsquedas del tesoro; por eso el valor se expone como "Valor".
// Cada fábrica aplica las reglas de su contexto; la igualdad es por valor.
public sealed class Tiempo : IEquatable<Tiempo>
{
    public const int MinimoPregunta = 5;
    public const int MaximoPregunta = 600;
    public const int MinimoBusqueda = 5;

    public int Valor { get; }

    private Tiempo(int valor) => Valor = valor;

    public static Tiempo CrearPositivo(int valor)
    {
        if (valor <= 0)
            throw new ExcepcionDominio("El tiempo debe ser mayor a cero.");
        return new Tiempo(valor);
    }

    public static Tiempo CrearParaPregunta(int segundos)
    {
        if (segundos < MinimoPregunta || segundos > MaximoPregunta)
            throw new ExcepcionDominio(
                $"El tiempo estimado debe estar entre {MinimoPregunta} y {MaximoPregunta} segundos.");
        return new Tiempo(segundos);
    }

    public static Tiempo CrearParaBusqueda(int minutos)
    {
        if (minutos < MinimoBusqueda)
            throw new ExcepcionDominio($"El tiempo debe ser al menos {MinimoBusqueda} minutos.");
        return new Tiempo(minutos);
    }

    // Rehidratación desde la base de datos: acepta datos existentes
    // (incluido 0 legacy o default de BD), pero nunca negativos.
    public static Tiempo DesdePersistencia(int valor)
    {
        if (valor < 0)
            throw new ExcepcionDominio("El tiempo no puede ser negativo.");
        return new Tiempo(valor);
    }

    public bool Equals(Tiempo? otro) => otro is not null && Valor == otro.Valor;

    public override bool Equals(object? obj) => Equals(obj as Tiempo);

    public override int GetHashCode() => Valor.GetHashCode();

    public static bool operator ==(Tiempo? izquierda, Tiempo? derecha) =>
        izquierda is null ? derecha is null : izquierda.Equals(derecha);

    public static bool operator !=(Tiempo? izquierda, Tiempo? derecha) =>
        !(izquierda == derecha);

    public override string ToString() => Valor.ToString();
}
