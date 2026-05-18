using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Dominio.ObjetosDeValor;

public sealed class NombrePersona : IEquatable<NombrePersona>
{
    public string Nombre { get; }
    public string Apellido { get; }

    private NombrePersona(string nombre, string apellido)
    {
        Nombre = nombre;
        Apellido = apellido;
    }

    public static NombrePersona Crear(string nombre, string apellido)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new DatosUsuarioInvalidosExcepcion("El nombre de la persona es obligatorio.");
        if (string.IsNullOrWhiteSpace(apellido))
            throw new DatosUsuarioInvalidosExcepcion("El apellido de la persona es obligatorio.");

        return new NombrePersona(nombre.Trim(), apellido.Trim());
    }

    public string Completo => $"{Nombre} {Apellido}".Trim();

    public bool Equals(NombrePersona? otro) =>
        otro is not null && Nombre == otro.Nombre && Apellido == otro.Apellido;
    public override bool Equals(object? obj) => obj is NombrePersona n && Equals(n);
    public override int GetHashCode() => HashCode.Combine(Nombre, Apellido);
}
