using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

// Composite — hoja: pista de ayuda que el Operador puede liberar en tiempo real durante una sesión.
public sealed class Pista : IComponenteJuego
{
    public Guid Id { get; private set; }
    public Guid BusquedaId { get; private set; }
    public string Contenido { get; private set; } = default!;

    private Pista() { }

    internal static Pista Crear(Guid busquedaId, string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            throw new ExcepcionDominio("El contenido de la pista es obligatorio.");

        return new Pista
        {
            Id = Guid.NewGuid(),
            BusquedaId = busquedaId,
            Contenido = contenido.Trim()
        };
    }

    internal void Modificar(string nuevoContenido)
    {
        if (string.IsNullOrWhiteSpace(nuevoContenido))
            throw new ExcepcionDominio("El contenido de la pista es obligatorio.");

        Contenido = nuevoContenido.Trim();
    }

    public static Pista Reconstituir(Guid id, Guid busquedaId, string contenido) =>
        new() { Id = id, BusquedaId = busquedaId, Contenido = contenido };

    string IComponenteJuego.ObtenerDescripcion() => $"Pista: {Contenido}";
    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos() =>
        Array.Empty<IComponenteJuego>();
}
