using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.Patrones;

namespace JuegosServicio.Dominio.Entidades;

// Composite — nodo compuesto de la jerarquía BusquedaTesoro → Etapa → Mision / Pista.
public sealed class Etapa : IComponenteJuego
{
    private readonly List<Mision> _misiones = new();
    private readonly List<Pista> _pistas = new();

    public Guid Id { get; private set; }
    public Guid BusquedaId { get; private set; }
    public string Titulo { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public int Orden { get; private set; }

    public IReadOnlyList<Mision> Misiones => _misiones.AsReadOnly();
    public IReadOnlyList<Pista> Pistas => _pistas.AsReadOnly();

    private Etapa() { }

    internal static Etapa Crear(Guid busquedaId, string titulo, string descripcion, int orden)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ExcepcionDominio("El título de la etapa es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la etapa es obligatoria.");
        if (orden <= 0)
            throw new ExcepcionDominio("El orden de la etapa debe ser mayor a cero.");

        return new Etapa
        {
            Id = Guid.NewGuid(),
            BusquedaId = busquedaId,
            Titulo = titulo.Trim(),
            Descripcion = descripcion.Trim(),
            Orden = orden
        };
    }

    internal void Modificar(string nuevoTitulo, string nuevaDescripcion, int nuevoOrden)
    {
        if (string.IsNullOrWhiteSpace(nuevoTitulo))
            throw new ExcepcionDominio("El título de la etapa es obligatorio.");
        if (string.IsNullOrWhiteSpace(nuevaDescripcion))
            throw new ExcepcionDominio("La descripción de la etapa es obligatoria.");
        if (nuevoOrden <= 0)
            throw new ExcepcionDominio("El orden de la etapa debe ser mayor a cero.");

        Titulo = nuevoTitulo.Trim();
        Descripcion = nuevaDescripcion.Trim();
        Orden = nuevoOrden;
    }

    internal Mision AgregarMision(
        string titulo,
        string descripcion,
        TipoMision tipo,
        string pistaClave)
    {
        var mision = Mision.Crear(Id, titulo, descripcion, tipo, pistaClave);
        _misiones.Add(mision);
        return mision;
    }

    internal void ModificarMision(Guid misionId, string nuevoTitulo, string nuevaDescripcion, TipoMision nuevoTipo, string nuevaPistaClave)
    {
        var mision = _misiones.FirstOrDefault(m => m.Id == misionId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la misión con ID '{misionId}'.");
        mision.Modificar(nuevoTitulo, nuevaDescripcion, nuevoTipo, nuevaPistaClave);
    }

    internal void EliminarMision(Guid misionId)
    {
        var mision = _misiones.FirstOrDefault(m => m.Id == misionId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la misión con ID '{misionId}'.");
        _misiones.Remove(mision);
    }

    // HU30 — modifica el contenido de una pista existente.
    internal void ModificarPista(Guid pistaId, string nuevoContenido)
    {
        var pista = _pistas.FirstOrDefault(p => p.Id == pistaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la pista con ID '{pistaId}'.");
        pista.Modificar(nuevoContenido);
    }

    // HU28 — agrega una pista de ayuda a esta etapa.
    internal Pista AgregarPista(string contenido)
    {
        var pista = Pista.Crear(Id, contenido);
        _pistas.Add(pista);
        return pista;
    }

    // IComponenteJuego — compuesto: devuelve misiones y pistas como hijos.
    string IComponenteJuego.ObtenerDescripcion() => $"Etapa {Orden}: {Titulo}";
    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos()
    {
        var hijos = new List<IComponenteJuego>();
        hijos.AddRange(_misiones.Cast<IComponenteJuego>());
        hijos.AddRange(_pistas.Cast<IComponenteJuego>());
        return hijos.AsReadOnly();
    }

    public static Etapa Reconstituir(
        Guid id,
        Guid busquedaId,
        string titulo,
        string descripcion,
        int orden,
        IEnumerable<Mision> misiones,
        IEnumerable<Pista>? pistas = null)
    {
        var etapa = new Etapa
        {
            Id = id,
            BusquedaId = busquedaId,
            Titulo = titulo,
            Descripcion = descripcion,
            Orden = orden
        };
        etapa._misiones.AddRange(misiones);
        if (pistas is not null) etapa._pistas.AddRange(pistas);
        return etapa;
    }
}
