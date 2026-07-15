using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

public sealed class Pista : IComponenteJuego
{
    public Guid Id { get; private set; }
    public Guid BusquedaId { get; private set; }
    public string Contenido { get; private set; } = string.Empty;
    public TipoPista Tipo { get; private set; }
    public double? Latitud { get; private set; }
    public double? Longitud { get; private set; }

    private Pista() { }

    internal static Pista Crear(Guid busquedaId, string? contenido, TipoPista tipo, double? latitud, double? longitud)
    {
        if (tipo == TipoPista.Texto && string.IsNullOrWhiteSpace(contenido))
            throw new ExcepcionDominio("El contenido de la pista es obligatorio.");
        if (tipo == TipoPista.CoordenadaGps && (!latitud.HasValue || !longitud.HasValue))
            throw new ExcepcionDominio("Las coordenadas GPS son obligatorias para una pista de tipo GPS.");

        return new Pista
        {
            Id = Guid.NewGuid(),
            BusquedaId = busquedaId,
            Contenido = contenido?.Trim() ?? string.Empty,
            Tipo = tipo,
            Latitud = latitud,
            Longitud = longitud
        };
    }

    internal void Modificar(string? nuevoContenido, TipoPista tipo, double? latitud, double? longitud)
    {
        if (tipo == TipoPista.Texto && string.IsNullOrWhiteSpace(nuevoContenido))
            throw new ExcepcionDominio("El contenido de la pista es obligatorio.");
        if (tipo == TipoPista.CoordenadaGps && (!latitud.HasValue || !longitud.HasValue))
            throw new ExcepcionDominio("Las coordenadas GPS son obligatorias para una pista de tipo GPS.");

        Contenido = nuevoContenido?.Trim() ?? string.Empty;
        Tipo = tipo;
        Latitud = latitud;
        Longitud = longitud;
    }

    public static Pista Reconstituir(Guid id, Guid busquedaId, string contenido, TipoPista tipo, double? latitud, double? longitud) =>
        new()
        {
            Id = id,
            BusquedaId = busquedaId,
            Contenido = contenido,
            Tipo = tipo,
            Latitud = latitud,
            Longitud = longitud
        };

    string IComponenteJuego.ObtenerDescripcion() => $"Pista: {Contenido}";
    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos() =>
        Array.Empty<IComponenteJuego>();
}
