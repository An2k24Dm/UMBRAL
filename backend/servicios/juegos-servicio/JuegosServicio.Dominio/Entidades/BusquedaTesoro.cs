using JuegosServicio.Dominio.Estados;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.Patrones;

namespace JuegosServicio.Dominio.Entidades;

// Composite — hoja: modo de juego de tipo búsqueda del tesoro.
// Contiene sus propias pistas de ayuda que el Operador puede liberar en
// tiempo real durante la sesión.
public sealed class BusquedaTesoro : IComponenteJuego
{
    private readonly List<Pista> _pistas = new();
    private readonly List<EventoDominio> _eventos = new();
    private IEstadoBusqueda _estado = default!;

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public Guid CreadorId { get; private set; }
    public Enums.EstadoBusqueda Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public int Tiempo { get; private set; }
    public int Puntaje { get; private set; }

    public IReadOnlyList<Pista> Pistas => _pistas.AsReadOnly();
    public IReadOnlyList<EventoDominio> Eventos => _eventos.AsReadOnly();

    private BusquedaTesoro() { }

    public static BusquedaTesoro Crear(
        string nombre,
        string descripcion,
        Guid creadorId,
        DateTime fechaCreacion,
        int tiempo = 0,
        int puntaje = 0)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ExcepcionDominio("El nombre de la búsqueda del tesoro es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la búsqueda del tesoro es obligatoria.");
        if (creadorId == Guid.Empty)
            throw new ExcepcionDominio("El identificador del creador es obligatorio.");
        if (tiempo < 0)
            throw new ExcepcionDominio("El tiempo no puede ser negativo.");
        if (puntaje < 0)
            throw new ExcepcionDominio("El puntaje no puede ser negativo.");

        var busqueda = new BusquedaTesoro
        {
            Id = Guid.NewGuid(),
            Nombre = nombre.Trim(),
            Descripcion = descripcion.Trim(),
            CreadorId = creadorId,
            Estado = Enums.EstadoBusqueda.Inactiva,
            FechaCreacion = fechaCreacion,
            Tiempo = tiempo,
            Puntaje = puntaje
        };
        busqueda._estado = FabricaEstadoBusqueda.Obtener(Enums.EstadoBusqueda.Inactiva);
        busqueda._eventos.Add(new BusquedaCreadaEvento(busqueda.Id, busqueda.Nombre));
        return busqueda;
    }

    // Las pistas se pueden agregar en cualquier estado para liberarlas
    // en tiempo real durante la sesión.
    public Pista AgregarPista(string contenido)
    {
        var pista = Pista.Crear(Id, contenido);
        _pistas.Add(pista);
        return pista;
    }

    public void ModificarPista(Guid pistaId, string nuevoContenido)
    {
        _estado.ValidarEdicion("modificar pistas");
        ObtenerPista(pistaId).Modificar(nuevoContenido);
    }

    public void EliminarPista(Guid pistaId)
    {
        _estado.ValidarEdicion("eliminar pistas");
        var pista = ObtenerPista(pistaId);
        _pistas.Remove(pista);
    }

    public void Modificar(string nombre, string descripcion, int tiempo, int puntaje)
    {
        _estado.ValidarEdicion("modificar la búsqueda");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ExcepcionDominio("El nombre de la búsqueda del tesoro es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la búsqueda del tesoro es obligatoria.");
        if (tiempo < 0)
            throw new ExcepcionDominio("El tiempo no puede ser negativo.");
        if (puntaje < 0)
            throw new ExcepcionDominio("El puntaje no puede ser negativo.");
        Nombre = nombre.Trim();
        Descripcion = descripcion.Trim();
        Tiempo = tiempo;
        Puntaje = puntaje;
    }

    public void Activar() => _estado.Activar(this);
    public void Desactivar() => _estado.Desactivar(this);

    string IComponenteJuego.ObtenerDescripcion() => $"Búsqueda: {Nombre} [{Estado}]";
    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos() =>
        _pistas.Cast<IComponenteJuego>().ToList().AsReadOnly();

    public void LimpiarEventos() => _eventos.Clear();

    public static BusquedaTesoro Reconstituir(
        Guid id,
        string nombre,
        string descripcion,
        Guid creadorId,
        Enums.EstadoBusqueda estado,
        DateTime fechaCreacion,
        int tiempo = 0,
        int puntaje = 0,
        IEnumerable<Pista>? pistas = null)
    {
        var busqueda = new BusquedaTesoro
        {
            Id = id,
            Nombre = nombre,
            Descripcion = descripcion,
            CreadorId = creadorId,
            Estado = estado,
            FechaCreacion = fechaCreacion,
            Tiempo = tiempo,
            Puntaje = puntaje
        };
        busqueda._estado = FabricaEstadoBusqueda.Obtener(estado);
        if (pistas is not null) busqueda._pistas.AddRange(pistas);
        return busqueda;
    }

    internal void TransicionarEstado(Enums.EstadoBusqueda nuevoEstado)
    {
        Estado = nuevoEstado;
        _estado = FabricaEstadoBusqueda.Obtener(nuevoEstado);
    }

    internal void AgregarEventoInterno(EventoDominio evento) => _eventos.Add(evento);

    private Pista ObtenerPista(Guid pistaId) =>
        _pistas.FirstOrDefault(p => p.Id == pistaId)
        ?? throw new ExcepcionNoEncontrado($"No se encontró la pista con ID '{pistaId}'.");
}
