using System.Text.RegularExpressions;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Dominio.ObjetosDeValor;

// Objeto de valor inmutable (record) — comparación por valor.
// Username de Keycloak (p. ej. "operador01"); NO es el correo.
public sealed record NombreUsuario
{
    private static readonly Regex Patron = new(
        @"^[a-z0-9._]{4,30}$",
        RegexOptions.Compiled);

    public string Valor { get; }

    private NombreUsuario(string valor) => Valor = valor;

    public static NombreUsuario Crear(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw new DatosUsuarioInvalidosExcepcion("El nombre de usuario es obligatorio.");

        var normalizado = valor.Trim().ToLowerInvariant();
        if (!Patron.IsMatch(normalizado))
            throw new DatosUsuarioInvalidosExcepcion(
                "El nombre de usuario debe tener entre 4 y 30 caracteres y solo puede contener letras, números, punto o guion bajo.");

        return new NombreUsuario(normalizado);
    }

    public override string ToString() => Valor;
}
