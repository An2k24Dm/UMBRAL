using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.Patrones;

namespace JuegosServicio.Dominio.Entidades;

// Patrón Composite — hoja de la jerarquía BusquedaTesoro → Etapa → Pista.
// Una pista es una ayuda textual que el Operador puede liberar a los
// participantes durante una sesión activa (HU42). Sin hijos propios.
public sealed class Pista : IComponenteJuego
{
    public Guid Id { get; private set; }
    public Guid EtapaId { get; private set; }
    public string Contenido { get; private set; } = default!;

    private Pista() { }

    internal static Pista Crear(Guid etapaId, string contenido)
    {
        if (string.IsNullOrWhiteSpace(contenido))
            throw new ExcepcionDominio("El contenido de la pista es obligatorio.");

        return new Pista
        {
            Id = Guid.NewGuid(),
            EtapaId = etapaId,
            Contenido = contenido.Trim()
        };
    }

    public static Pista Reconstituir(Guid id, Guid etapaId, string contenido)
    {
        return new Pista { Id = id, EtapaId = etapaId, Contenido = contenido };
    }

    // IComponenteJuego — hoja: sin hijos.
    string IComponenteJuego.ObtenerDescripcion() => $"Pista: {Contenido}";
    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos() =>
        Array.Empty<IComponenteJuego>();
}
