using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Estados;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

public sealed class BusquedaTesoro
{
    private readonly List<Etapa> _etapas = new();
    private readonly List<EventoDominio> _eventos = new();
    private IEstadoBusqueda _estado = default!;

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public Guid CreadorId { get; private set; }
    public EstadoBusqueda Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    public IReadOnlyList<Etapa> Etapas => _etapas.AsReadOnly();
    public IReadOnlyList<EventoDominio> Eventos => _eventos.AsReadOnly();

    private BusquedaTesoro() { }

    public static BusquedaTesoro Crear(
        string nombre,
        string descripcion,
        Guid creadorId,
        DateTime fechaCreacion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ExcepcionDominio("El nombre de la búsqueda del tesoro es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la búsqueda del tesoro es obligatoria.");
        if (creadorId == Guid.Empty)
            throw new ExcepcionDominio("El identificador del creador es obligatorio.");

        var busqueda = new BusquedaTesoro
        {
            Id = Guid.NewGuid(),
            Nombre = nombre.Trim(),
            Descripcion = descripcion.Trim(),
            CreadorId = creadorId,
            Estado = EstadoBusqueda.Inactiva,
            FechaCreacion = fechaCreacion
        };
        busqueda._estado = FabricaEstadoBusqueda.Obtener(EstadoBusqueda.Inactiva);

        busqueda._eventos.Add(new BusquedaCreadaEvento(busqueda.Id, busqueda.Nombre));
        return busqueda;
    }

    public Etapa AgregarEtapa(string titulo, string descripcion)
    {
        _estado.ValidarEdicion("agregar etapas");

        var orden = _etapas.Count + 1;
        var etapa = Etapa.Crear(Id, titulo, descripcion, orden);
        _etapas.Add(etapa);
        return etapa;
    }

    // Patrón State: delega en el objeto de estado actual.
    public void Activar() => _estado.Activar(this);
    public void Desactivar() => _estado.Desactivar(this);

    // HU28 — agrega una pista de ayuda a una etapa específica.
    // Las pistas pueden agregarse en cualquier estado (Inactiva o Activa)
    // porque el Operador puede liberarlas en tiempo real durante una sesión.
    public Pista AgregarPistaAEtapa(Guid etapaId, string contenido)
    {
        var etapa = _etapas.FirstOrDefault(e => e.Id == etapaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la etapa con ID '{etapaId}'.");

        return etapa.AgregarPista(contenido);
    }

    public void ModificarEtapa(Guid etapaId, string nuevoTitulo, string nuevaDescripcion, int nuevoOrden)
    {
        _estado.ValidarEdicion("modificar etapas");

        var etapa = _etapas.FirstOrDefault(e => e.Id == etapaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la etapa con ID '{etapaId}'.");

        if (nuevoOrden != etapa.Orden && _etapas.Any(e => e.Id != etapaId && e.Orden == nuevoOrden))
            throw new ExcepcionDominio(
                $"Ya existe una etapa con el orden {nuevoOrden} en esta búsqueda del tesoro.");

        etapa.Modificar(nuevoTitulo, nuevaDescripcion, nuevoOrden);
    }

    public void EliminarEtapa(Guid etapaId)
    {
        _estado.ValidarEdicion("eliminar etapas");

        var etapa = _etapas.FirstOrDefault(e => e.Id == etapaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la etapa con ID '{etapaId}'.");

        _etapas.Remove(etapa);
    }

    public Mision AgregarMisionAEtapa(
        Guid etapaId,
        string titulo,
        string descripcion,
        TipoMision tipo,
        string pistaClave)
    {
        _estado.ValidarEdicion("agregar misiones");

        var etapa = _etapas.FirstOrDefault(e => e.Id == etapaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la etapa con ID '{etapaId}'.");

        return etapa.AgregarMision(titulo, descripcion, tipo, pistaClave);
    }

    public void ModificarMision(Guid etapaId, Guid misionId, string nuevoTitulo, string nuevaDescripcion, TipoMision nuevoTipo, string nuevaPistaClave)
    {
        _estado.ValidarEdicion("modificar misiones");

        var etapa = _etapas.FirstOrDefault(e => e.Id == etapaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la etapa con ID '{etapaId}'.");

        etapa.ModificarMision(misionId, nuevoTitulo, nuevaDescripcion, nuevoTipo, nuevaPistaClave);
    }

    public void EliminarMision(Guid etapaId, Guid misionId)
    {
        _estado.ValidarEdicion("eliminar misiones");

        var etapa = _etapas.FirstOrDefault(e => e.Id == etapaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la etapa con ID '{etapaId}'.");

        etapa.EliminarMision(misionId);
    }

    public void LimpiarEventos() => _eventos.Clear();

    public static BusquedaTesoro Reconstituir(
        Guid id,
        string nombre,
        string descripcion,
        Guid creadorId,
        EstadoBusqueda estado,
        DateTime fechaCreacion,
        IEnumerable<Etapa> etapas)
    {
        var busqueda = new BusquedaTesoro
        {
            Id = id,
            Nombre = nombre,
            Descripcion = descripcion,
            CreadorId = creadorId,
            Estado = estado,
            FechaCreacion = fechaCreacion
        };
        busqueda._estado = FabricaEstadoBusqueda.Obtener(estado);
        busqueda._etapas.AddRange(etapas);
        return busqueda;
    }

    // Métodos internos para uso exclusivo de los estados (patrón State).
    internal void TransicionarEstado(EstadoBusqueda nuevoEstado)
    {
        Estado = nuevoEstado;
        _estado = FabricaEstadoBusqueda.Obtener(nuevoEstado);
    }

    internal void AgregarEventoInterno(EventoDominio evento) => _eventos.Add(evento);
}
