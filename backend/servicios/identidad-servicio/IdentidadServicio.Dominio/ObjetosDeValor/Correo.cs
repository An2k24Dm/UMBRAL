using System.Text.RegularExpressions;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Dominio.ObjetosDeValor;

// Objeto de valor inmutable (record) — comparación por valor.
// Correo electrónico independiente; NO está dentro de DatosContacto.
public sealed record Correo
{
    private static readonly Regex Patron = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Valor { get; }

    private Correo(string valor) => Valor = valor;

    public static Correo Crear(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DatosUsuarioInvalidosExcepcion("El correo es obligatorio.");

        var normalizado = valor.Trim().ToLowerInvariant();
        if (!Patron.IsMatch(normalizado))
            throw new DatosUsuarioInvalidosExcepcion(
                $"El correo '{valor}' no tiene un formato válido.");

        return new Correo(normalizado);
    }

    public override string ToString() => Valor;
}
