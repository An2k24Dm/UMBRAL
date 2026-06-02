using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.Patrones;

namespace JuegosServicio.Dominio.Entidades;

// Composite — nodo: misión única de una BusquedaTesoro. Sus hojas son las Pistas de ayuda.
public sealed class Mision : IComponenteJuego
{
    private readonly List<Pista> _pistas = new();

    public Guid Id { get; private set; }
    public Guid BusquedaId { get; private set; }
    public string Titulo { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public TipoMision Tipo { get; private set; }
    public string PistaClave { get; private set; } = default!;

    public IReadOnlyList<Pista> Pistas => _pistas.AsReadOnly();

    private Mision() { }

    string IComponenteJuego.ObtenerDescripcion() => $"Misión: {Titulo} [{Tipo}]";
    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos() =>
        _pistas.Cast<IComponenteJuego>().ToList().AsReadOnly();

    internal static Mision Crear(Guid busquedaId, string titulo, string descripcion, TipoMision tipo, string pistaClave)
    {
        if (string.IsNullOrWhiteSpace(titulo))
            throw new ExcepcionDominio("El título de la misión es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la misión es obligatoria.");
        if (string.IsNullOrWhiteSpace(pistaClave))
            throw new ExcepcionDominio("La pista clave de la misión es obligatoria.");

        return new Mision
        {
            Id = Guid.NewGuid(),
            BusquedaId = busquedaId,
            Titulo = titulo.Trim(),
            Descripcion = descripcion.Trim(),
            Tipo = tipo,
            PistaClave = pistaClave.Trim()
        };
    }

    internal void Modificar(string nuevoTitulo, string nuevaDescripcion, TipoMision nuevoTipo, string nuevaPistaClave)
    {
        if (string.IsNullOrWhiteSpace(nuevoTitulo))
            throw new ExcepcionDominio("El título de la misión es obligatorio.");
        if (string.IsNullOrWhiteSpace(nuevaDescripcion))
            throw new ExcepcionDominio("La descripción de la misión es obligatoria.");
        if (string.IsNullOrWhiteSpace(nuevaPistaClave))
            throw new ExcepcionDominio("La pista clave de la misión es obligatoria.");

        Titulo = nuevoTitulo.Trim();
        Descripcion = nuevaDescripcion.Trim();
        Tipo = nuevoTipo;
        PistaClave = nuevaPistaClave.Trim();
    }

    // Las pistas se pueden agregar en cualquier estado (Activa o Inactiva) para liberarlas en tiempo real.
    internal Pista AgregarPista(string contenido)
    {
        var pista = Pista.Crear(Id, contenido);
        _pistas.Add(pista);
        return pista;
    }

    internal void ModificarPista(Guid pistaId, string nuevoContenido)
    {
        var pista = _pistas.FirstOrDefault(p => p.Id == pistaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la pista con ID '{pistaId}'.");
        pista.Modificar(nuevoContenido);
    }

    internal void EliminarPista(Guid pistaId)
    {
        var pista = _pistas.FirstOrDefault(p => p.Id == pistaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la pista con ID '{pistaId}'.");
        _pistas.Remove(pista);
    }

    public static Mision Reconstituir(
        Guid id,
        Guid busquedaId,
        string titulo,
        string descripcion,
        TipoMision tipo,
        string pistaClave,
        IEnumerable<Pista>? pistas = null)
    {
        var mision = new Mision
        {
            Id = id,
            BusquedaId = busquedaId,
            Titulo = titulo,
            Descripcion = descripcion,
            Tipo = tipo,
            PistaClave = pistaClave
        };
        if (pistas is not null) mision._pistas.AddRange(pistas);
        return mision;
    }
}
