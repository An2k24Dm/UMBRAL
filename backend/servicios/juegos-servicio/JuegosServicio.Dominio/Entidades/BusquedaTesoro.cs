using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;
using JuegosServicio.Dominio.ObjetosValor;

namespace JuegosServicio.Dominio.Entidades;

public sealed class BusquedaTesoro : IComponenteJuego
{
    private readonly List<Pista> _pistas = new();
    private readonly List<EventoDominio> _eventos = new();
    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public Guid CreadorId { get; private set; }
    public EstadoBusqueda Estado { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public Tiempo Tiempo { get; private set; } = default!;
    public Puntaje Puntaje { get; private set; } = default!;
    public string CodigoQr { get; private set; } = default!;
    public IReadOnlyList<Pista> Pistas => _pistas.AsReadOnly();
    public IReadOnlyList<EventoDominio> Eventos => _eventos.AsReadOnly();

    private BusquedaTesoro() { }

    public static BusquedaTesoro Crear(
        string nombre,
        string descripcion,
        Guid creadorId,
        DateTime fechaCreacion,
        Tiempo? tiempo = null,
        Puntaje? puntaje = null)
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
            FechaCreacion = fechaCreacion,
            Tiempo = tiempo ?? Tiempo.CrearParaBusqueda(Tiempo.MinimoBusqueda),
            Puntaje = puntaje ?? Puntaje.Cero,
            CodigoQr = Guid.NewGuid().ToString("N")
        };
        busqueda._eventos.Add(new BusquedaCreadaEvento(busqueda.Id, busqueda.Nombre));
        return busqueda;
    }

    public Pista AgregarPista(string? contenido, Enums.TipoPista tipo, double? latitud, double? longitud)
    {
        if (Estado == EstadoBusqueda.Activa)
            throw new ExcepcionDominio("No se pueden agregar pistas a una búsqueda que está activa.");
        if (tipo == Enums.TipoPista.CoordenadaGps && _pistas.Any(p => p.Tipo == Enums.TipoPista.CoordenadaGps))
            throw new ExcepcionDominio("La búsqueda del tesoro ya tiene una coordenada GPS definida. Solo se permite una.");
        var pista = Pista.Crear(Id, contenido, tipo, latitud, longitud);
        _pistas.Add(pista);
        return pista;
    }

    public void ModificarPista(Guid pistaId, string? nuevoContenido, Enums.TipoPista tipo, double? latitud, double? longitud)
    {
        if (Estado == EstadoBusqueda.Activa)
            throw new ExcepcionDominio("No se pueden modificar pistas a una búsqueda que está activa.");
        ObtenerPista(pistaId).Modificar(nuevoContenido, tipo, latitud, longitud);
    }

    public void EliminarPista(Guid pistaId)
    {
        if (Estado == EstadoBusqueda.Activa)
            throw new ExcepcionDominio("No se pueden eliminar pistas a una búsqueda que está activa.");
        var pista = ObtenerPista(pistaId);
        _pistas.Remove(pista);
    }

    public void Modificar(string nombre, string descripcion, Tiempo tiempo, Puntaje puntaje)
    {
        if (Estado == EstadoBusqueda.Activa)
            throw new ExcepcionDominio("No se puede modificar una búsqueda que está activa.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ExcepcionDominio("El nombre de la búsqueda del tesoro es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la búsqueda del tesoro es obligatoria.");
        if (tiempo is null)
            throw new ExcepcionDominio("El tiempo de la búsqueda es obligatorio.");
        if (puntaje is null)
            throw new ExcepcionDominio("El puntaje de la búsqueda es obligatorio.");
        Nombre = nombre.Trim();
        Descripcion = descripcion.Trim();
        Tiempo = tiempo;
        Puntaje = puntaje;
    }

    public void Activar()
    {
        if (Estado == EstadoBusqueda.Activa)
            throw new ExcepcionDominio("La búsqueda del tesoro ya está activa.");
        if (!_pistas.Any(p => p.Tipo == Enums.TipoPista.CoordenadaGps))
            throw new ExcepcionDominio("La búsqueda del tesoro debe tener una coordenada GPS del tesoro para poder activarse.");

        Estado = EstadoBusqueda.Activa;
        _eventos.Add(new BusquedaActivadaEvento(Id, Nombre));
    }

    public void Desactivar()
    {
        if (Estado == EstadoBusqueda.Inactiva)
            throw new ExcepcionDominio("La búsqueda del tesoro ya está inactiva.");

        Estado = EstadoBusqueda.Inactiva;
        _eventos.Add(new BusquedaArchivadaEvento(Id));
    }

    string IComponenteJuego.ObtenerDescripcion() => $"Búsqueda: {Nombre} [{Estado}]";
    IReadOnlyList<IComponenteJuego> IComponenteJuego.ObtenerHijos() =>
        _pistas.Cast<IComponenteJuego>().ToList().AsReadOnly();

    public void LimpiarEventos() => _eventos.Clear();

    // Reconstituir NO re-valida rangos: rehidrata los enteros ya persistidos
    // con DesdePersistencia (que admite 0 legacy) para no romper
    // listados/detalles de registros existentes.
    public static BusquedaTesoro Reconstituir(
        Guid id,
        string nombre,
        string descripcion,
        Guid creadorId,
        EstadoBusqueda estado,
        DateTime fechaCreacion,
        int tiempo = 0,
        int puntaje = 0,
        IEnumerable<Pista>? pistas = null,
        string codigoQr = "")
    {
        var busqueda = new BusquedaTesoro
        {
            Id = id,
            Nombre = nombre,
            Descripcion = descripcion,
            CreadorId = creadorId,
            Estado = estado,
            FechaCreacion = fechaCreacion,
            Tiempo = ObjetosValor.Tiempo.DesdePersistencia(tiempo),
            Puntaje = ObjetosValor.Puntaje.DesdePersistencia(puntaje),
            CodigoQr = codigoQr
        };
        if (pistas is not null) busqueda._pistas.AddRange(pistas);
        return busqueda;
    }

    private Pista ObtenerPista(Guid pistaId) =>
        _pistas.FirstOrDefault(p => p.Id == pistaId)
        ?? throw new ExcepcionNoEncontrado($"No se encontró la pista con ID '{pistaId}'.");
}
