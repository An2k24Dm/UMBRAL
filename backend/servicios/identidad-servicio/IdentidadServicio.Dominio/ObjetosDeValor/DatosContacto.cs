namespace IdentidadServicio.Dominio.ObjetosDeValor;

public sealed class DatosContacto : IEquatable<DatosContacto>
{
    public string? Direccion { get; }
    public string? Telefono { get; }

    private DatosContacto(string? direccion, string? telefono)
    {
        Direccion = direccion;
        Telefono = telefono;
    }

    public static DatosContacto Crear(string? direccion, string? telefono)
    {
        return new DatosContacto(
            direccion: string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim(),
            telefono: string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim());
    }

    public static DatosContacto Vacio() => new(null, null);

    public bool Equals(DatosContacto? otro) =>
        otro is not null && Direccion == otro.Direccion && Telefono == otro.Telefono;
    public override bool Equals(object? obj) => obj is DatosContacto d && Equals(d);
    public override int GetHashCode() => HashCode.Combine(Direccion, Telefono);
}
