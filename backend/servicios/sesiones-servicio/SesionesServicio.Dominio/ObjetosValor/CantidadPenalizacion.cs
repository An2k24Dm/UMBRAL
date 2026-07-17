using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.ObjetosValor;

public sealed class CantidadPenalizacion : IEquatable<CantidadPenalizacion>
{
    public const int Minimo = 1;
    public const int Maximo = 100;

    public int Valor { get; }

    private CantidadPenalizacion(int valor) => Valor = valor;

    public static CantidadPenalizacion Crear(int valor)
    {
        if (valor < Minimo || valor > Maximo)
            throw new PenalizacionInvalidaExcepcion(
                $"La penalización debe ser un entero entre {Minimo} y {Maximo}.");
        return new CantidadPenalizacion(valor);
    }

    public bool Equals(CantidadPenalizacion? otro) => otro is not null && Valor == otro.Valor;

    public override bool Equals(object? obj) => Equals(obj as CantidadPenalizacion);

    public override int GetHashCode() => Valor.GetHashCode();

    public override string ToString() => Valor.ToString();
}
