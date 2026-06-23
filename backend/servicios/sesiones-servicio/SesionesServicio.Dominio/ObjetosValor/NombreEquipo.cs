using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.ObjetosValor;

// Value Object que encapsula el nombre de un equipo. Normaliza (Trim),
// rechaza valores vacíos y limita la longitud al máximo que admite la
// persistencia (HasMaxLength(80)). Igualdad por valor, case-insensitive,
// coherente con la unicidad de nombre dentro de la sesión.
public sealed class NombreEquipo : IEquatable<NombreEquipo>
{
    public const int LongitudMaxima = 80;

    public string Valor { get; }

    private NombreEquipo(string valor)
    {
        Valor = valor;
    }

    public static NombreEquipo Crear(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new EquipoInvalidoExcepcion("El nombre del equipo es obligatorio.");

        var normalizado = valor.Trim();

        if (normalizado.Length > LongitudMaxima)
            throw new EquipoInvalidoExcepcion(
                $"El nombre del equipo no puede superar {LongitudMaxima} caracteres.");

        return new NombreEquipo(normalizado);
    }

    public bool Equals(NombreEquipo? otro)
        => otro is not null
           && string.Equals(Valor, otro.Valor, StringComparison.OrdinalIgnoreCase);

    public override bool Equals(object? obj) => Equals(obj as NombreEquipo);

    public override int GetHashCode()
        => StringComparer.OrdinalIgnoreCase.GetHashCode(Valor);

    public override string ToString() => Valor;
}
