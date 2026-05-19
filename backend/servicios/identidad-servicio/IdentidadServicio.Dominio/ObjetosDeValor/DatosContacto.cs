using System.Text.RegularExpressions;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Dominio.ObjetosDeValor;

// Objeto de valor inmutable (record) — comparación por valor.
// Dirección y teléfono ahora son obligatorios. NO contiene Correo (Correo es VO
// independiente). Teléfono se normaliza sin espacios ni guiones.
public sealed record DatosContacto
{
    private static readonly Regex RegexTelefonoDigitos =
        new(@"^\d+$", RegexOptions.Compiled);

    private static readonly string[] CodigosTelefonoValidos =
        { "0414", "0412", "0424", "0416", "0426", "0212" };

    public string Direccion { get; }
    public string Telefono { get; }

    private DatosContacto(string direccion, string telefono)
    {
        Direccion = direccion;
        Telefono = telefono;
    }

    public static DatosContacto Crear(string direccion, string telefono)
    {
        var dir = direccion?.Trim() ?? string.Empty;
        if (dir.Length == 0)
            throw new DatosUsuarioInvalidosExcepcion("La dirección es obligatoria.");
        if (dir.Length < 5)
            throw new DatosUsuarioInvalidosExcepcion(
                "La dirección debe tener al menos 5 caracteres.");

        var tel = NormalizarTelefono(telefono);
        if (string.IsNullOrEmpty(tel))
            throw new DatosUsuarioInvalidosExcepcion("El teléfono es obligatorio.");
        if (!RegexTelefonoDigitos.IsMatch(tel))
            throw new DatosUsuarioInvalidosExcepcion(
                "El teléfono debe contener solo números.");
        if (tel.Length != 11)
            throw new DatosUsuarioInvalidosExcepcion("El teléfono debe tener 11 dígitos.");
        if (!CodigosTelefonoValidos.Any(c => tel.StartsWith(c)))
            throw new DatosUsuarioInvalidosExcepcion(
                "El teléfono debe comenzar con un código válido, por ejemplo 0414, 0212, 0424 o 0412.");

        return new DatosContacto(dir, tel);
    }

    private static string NormalizarTelefono(string? telefono)
    {
        if (string.IsNullOrWhiteSpace(telefono)) return string.Empty;
        return new string(telefono.Where(c => !char.IsWhiteSpace(c) && c != '-').ToArray());
    }

    public override string ToString() => $"{Direccion} — {Telefono}";
}
