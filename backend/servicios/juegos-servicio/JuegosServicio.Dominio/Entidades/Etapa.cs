using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

public sealed class Etapa
{
    private readonly List<Mision> _misiones = new();

    public Guid Id { get; private set; }
    public Guid BusquedaId { get; private set; }
    public string Titulo { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public int Orden { get; private set; }

    public IReadOnlyList<Mision> Misiones => _misiones.AsReadOnly();

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

    internal void Modificar(string nuevoTitulo, string nuevaDescripcion)
    {
        if (string.IsNullOrWhiteSpace(nuevoTitulo))
            throw new ExcepcionDominio("El título de la etapa es obligatorio.");
        if (string.IsNullOrWhiteSpace(nuevaDescripcion))
            throw new ExcepcionDominio("La descripción de la etapa es obligatoria.");

        Titulo = nuevoTitulo.Trim();
        Descripcion = nuevaDescripcion.Trim();
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

    public static Etapa Reconstituir(
        Guid id,
        Guid busquedaId,
        string titulo,
        string descripcion,
        int orden,
        IEnumerable<Mision> misiones)
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
        return etapa;
    }
}
