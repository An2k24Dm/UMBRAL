using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.Patrones;

namespace JuegosServicio.Dominio.Entidades;

// Composite — hoja: pista de ayuda que el Operador puede liberar en tiempo real durante una sesión.
public sealed class Pista : IComponenteJuego
{
    public Guid Id { get; private set; }
    public Guid MisionId { get; private set; }
    public string Contenido { get; private set; } = default!;

    private Pista() { }

    internal static Pista Crear(Guid misionId, string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            throw new ExcepcionDominio("El contenido de la pista es obligatorio.");

        return new Pista
        {
            Id = Guid.NewGuid(),
            MisionId = misionId,
            Contenido = contenido.Trim()
        };
    }

    internal void Modificar(string nuevoContenido)
    {
        if (string.IsNullOrWhiteSpace(nuevoContenido))
            throw new ExcepcionDominio("El contenido de la pista es obligatorio.");

        Contenido = nuevoContenido.Trim();
    }

    public static Pista Reconstituir(Guid id, Guid misionId, string contenido)
    {
        return new Pista { Id = id, MisionId = misionId, Contenido = contenido };
    }

    string IComponenteJuego.ObtenerDescripcion() => $"Pista: {Contenido}";
    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos() =>
        Array.Empty<IComponenteJuego>();
}
