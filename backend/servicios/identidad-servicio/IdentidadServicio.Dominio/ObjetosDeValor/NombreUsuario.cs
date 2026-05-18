using System.Text.RegularExpressions;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Dominio.ObjetosDeValor;

public sealed class NombreUsuario : IEquatable<NombreUsuario>
{
    private static readonly Regex Patron = new(
        @"^[a-z0-9._-]{3,50}$",
        RegexOptions.Compiled);

    public string Valor { get; }

    private NombreUsuario(string valor) => Valor = valor;

    public static NombreUsuario Crear(string valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DatosUsuarioInvalidosExcepcion("El nombre de usuario es obligatorio.");
        }

        var normalizado = valor.Trim().ToLowerInvariant();
        if (!Patron.IsMatch(normalizado))
        {
            throw new DatosUsuarioInvalidosExcepcion(
                "El nombre de usuario debe tener entre 3 y 50 caracteres y solo puede contener letras, números, punto, guion y guion bajo.");
        }

        return new NombreUsuario(normalizado);
    }

    public bool Equals(NombreUsuario? otro) => otro is not null && Valor == otro.Valor;
    public override bool Equals(object? obj) => obj is NombreUsuario n && Equals(n);
    public override int GetHashCode() => Valor.GetHashCode();
    public override string ToString() => Valor;
}
