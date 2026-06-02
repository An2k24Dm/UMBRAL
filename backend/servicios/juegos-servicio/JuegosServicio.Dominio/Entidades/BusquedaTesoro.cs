using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Estados;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.Patrones;

namespace JuegosServicio.Dominio.Entidades;

// Composite — raíz: BusquedaTesoro → Mision → Pistas.
public sealed class BusquedaTesoro : IComponenteJuego
{
    private readonly List<EventoDominio> _eventos = new();
    private IEstadoBusqueda _estado = default!;

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public Guid CreadorId { get; private set; }
    public EstadoBusqueda Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public Mision? Mision { get; private set; }

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

    public Mision AsignarMision(string titulo, string descripcion, TipoMision tipo, string pistaClave)
    {
        _estado.ValidarEdicion("asignar misión");

        if (Mision is not null)
            throw new ExcepcionDominio("La búsqueda del tesoro ya tiene una misión asignada. Modifíquela en lugar de crear una nueva.");

        Mision = Entidades.Mision.Crear(Id, titulo, descripcion, tipo, pistaClave);
        return Mision;
    }

    public void ModificarMision(string nuevoTitulo, string nuevaDescripcion, TipoMision nuevoTipo, string nuevaPistaClave)
    {
        _estado.ValidarEdicion("modificar misión");

        if (Mision is null)
            throw new ExcepcionNoEncontrado("La búsqueda del tesoro no tiene una misión asignada.");

        Mision.Modificar(nuevoTitulo, nuevaDescripcion, nuevoTipo, nuevaPistaClave);
    }

    public void EliminarMision()
    {
        _estado.ValidarEdicion("eliminar misión");

        if (Mision is null)
            throw new ExcepcionNoEncontrado("La búsqueda del tesoro no tiene una misión asignada.");

        Mision = null;
    }

    // Las pistas se pueden agregar en cualquier estado para liberarlas en tiempo real durante la sesión.
    public Pista AgregarPistaAMision(string contenido)
    {
        if (Mision is null)
            throw new ExcepcionNoEncontrado("La búsqueda del tesoro no tiene una misión asignada.");

        return Mision.AgregarPista(contenido);
    }

    public void ModificarPista(Guid pistaId, string nuevoContenido)
    {
        _estado.ValidarEdicion("modificar pistas");

        if (Mision is null)
            throw new ExcepcionNoEncontrado("La búsqueda del tesoro no tiene una misión asignada.");

        Mision.ModificarPista(pistaId, nuevoContenido);
    }

    public void EliminarPista(Guid pistaId)
    {
        _estado.ValidarEdicion("eliminar pistas");

        if (Mision is null)
            throw new ExcepcionNoEncontrado("La búsqueda del tesoro no tiene una misión asignada.");

        Mision.EliminarPista(pistaId);
    }

    public void Activar() => _estado.Activar(this);
    public void Desactivar() => _estado.Desactivar(this);

    string IComponenteJuego.ObtenerDescripcion() => $"Búsqueda: {Nombre} [{Estado}]";
    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos() =>
        Mision is null
            ? Array.Empty<IComponenteJuego>()
            : new List<IComponenteJuego> { Mision }.AsReadOnly();

    public void LimpiarEventos() => _eventos.Clear();

    public static BusquedaTesoro Reconstituir(
        Guid id,
        string nombre,
        string descripcion,
        Guid creadorId,
        EstadoBusqueda estado,
        DateTime fechaCreacion,
        Mision? mision)
    {
        var busqueda = new BusquedaTesoro
        {
            Id = id,
            Nombre = nombre,
            Descripcion = descripcion,
            CreadorId = creadorId,
            Estado = estado,
            FechaCreacion = fechaCreacion,
            Mision = mision
        };
        busqueda._estado = FabricaEstadoBusqueda.Obtener(estado);
        return busqueda;
    }

    internal void TransicionarEstado(EstadoBusqueda nuevoEstado)
    {
        Estado = nuevoEstado;
        _estado = FabricaEstadoBusqueda.Obtener(nuevoEstado);
    }

    internal void AgregarEventoInterno(EventoDominio evento) => _eventos.Add(evento);
}
