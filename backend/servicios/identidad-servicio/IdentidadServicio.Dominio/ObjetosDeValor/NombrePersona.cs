using System.Text.RegularExpressions;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Dominio.ObjetosDeValor;

// Objeto de valor inmutable (record) — comparación por valor.
// Acepta letras (incluye acentos y ñ) y espacios; longitud 2-50 por parte.
public sealed record NombrePersona
{
    private static readonly Regex Patron = new(
        @"^[A-Za-zÁÉÍÓÚáéíóúÑñÜü\s]{2,50}$",
        RegexOptions.Compiled);

    public string Nombre { get; }
    public string Apellido { get; }

    private NombrePersona(string nombre, string apellido)
    {
        Nombre = nombre;
        Apellido = apellido;
    }

    public static NombrePersona Crear(string nombre, string apellido)
    {
        var n = nombre?.Trim() ?? string.Empty;
        var a = apellido?.Trim() ?? string.Empty;

        if (n.Length == 0)
            throw new DatosUsuarioInvalidosExcepcion("El nombre de la persona es obligatorio.");
        if (!Patron.IsMatch(n))
            throw new DatosUsuarioInvalidosExcepcion(
                "El nombre solo puede contener letras y espacios (2 a 50 caracteres).");

        if (a.Length == 0)
            throw new DatosUsuarioInvalidosExcepcion("El apellido de la persona es obligatorio.");
        if (!Patron.IsMatch(a))
            throw new DatosUsuarioInvalidosExcepcion(
                "El apellido solo puede contener letras y espacios (2 a 50 caracteres).");

        return new NombrePersona(n, a);
    }

    public string ObtenerNombreCompleto() => $"{Nombre} {Apellido}".Trim();

    // Alias por retrocompatibilidad con código que usaba esta propiedad.
    public string Completo => ObtenerNombreCompleto();

    public override string ToString() => ObtenerNombreCompleto();
}
