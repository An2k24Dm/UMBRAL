using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.ObjetosValor;

// Value Object que encapsula el puntaje obtenido durante la ejecución de una
// sesión (participantes y equipos). Es un entero nunca negativo; cero es el
// puntaje inicial válido. Inmutable: Sumar devuelve una nueva instancia.
// Igualdad por valor. El cálculo de ranking/posiciones corresponde a
// ranking-servicio; aquí solo se garantiza que el dato persistido sea válido.
public sealed class PuntajeSesion : IEquatable<PuntajeSesion>
{
    private static readonly PuntajeSesion CeroCompartido = new(0);

    public int Valor { get; }

    private PuntajeSesion(int valor) => Valor = valor;

    public static PuntajeSesion Cero() => CeroCompartido;

    public static PuntajeSesion Crear(int valor)
    {
        if (valor < 0)
            throw new ParticipacionInvalidaExcepcion("El puntaje no puede ser negativo.");
        return new PuntajeSesion(valor);
    }

    // Rehidratación desde la base de datos: aplica el mismo límite (nunca
    // negativo); cero es válido como estado inicial persistido.
    public static PuntajeSesion DesdePersistencia(int valor) => Crear(valor);

    public PuntajeSesion Sumar(PuntajeSesion otro)
    {
        if (otro is null)
            throw new ParticipacionInvalidaExcepcion("El puntaje a sumar es obligatorio.");
        return new PuntajeSesion(Valor + otro.Valor);
    }

    public PuntajeSesion Sumar(int puntos)
    {
        if (puntos < 0)
            throw new ParticipacionInvalidaExcepcion(
                "El puntaje a sumar no puede ser negativo.");
        return new PuntajeSesion(Valor + puntos);
    }

    public bool Equals(PuntajeSesion? otro) => otro is not null && Valor == otro.Valor;

    public override bool Equals(object? obj) => Equals(obj as PuntajeSesion);

    public override int GetHashCode() => Valor.GetHashCode();

    public static bool operator ==(PuntajeSesion? izquierda, PuntajeSesion? derecha) =>
        izquierda is null ? derecha is null : izquierda.Equals(derecha);

    public static bool operator !=(PuntajeSesion? izquierda, PuntajeSesion? derecha) =>
        !(izquierda == derecha);

    public override string ToString() => Valor.ToString();
}
