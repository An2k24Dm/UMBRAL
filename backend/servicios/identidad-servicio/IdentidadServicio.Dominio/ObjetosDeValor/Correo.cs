using System.Text.RegularExpressions;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Dominio.ObjetosDeValor;

// Correo electrónico como objeto de valor independiente.
// Es distinto del NombreUsuario: NombreUsuario es el username de Keycloak
// (p. ej. "operador01"); Correo es el email del usuario (p. ej.
// "operador@umbral.com"). Ambos viajan separados al backend y a Keycloak.
public sealed class Correo : IEquatable<Correo>
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

    public bool Equals(Correo? otro) => otro is not null && Valor == otro.Valor;
    public override bool Equals(object? obj) => obj is Correo c && Equals(c);
    public override int GetHashCode() => Valor.GetHashCode();
    public override string ToString() => Valor;
}
