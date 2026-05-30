using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Eventos;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

public sealed class BusquedaTesoro
{
    private readonly List<Etapa> _etapas = new();
    private readonly List<EventoDominio> _eventos = new();

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

        busqueda._eventos.Add(new BusquedaCreadaEvento(busqueda.Id, busqueda.Nombre));
        return busqueda;
    }

    public Etapa AgregarEtapa(string titulo, string descripcion)
    {
        ValidarEstadoInactiva("agregar etapas");

        var orden = _etapas.Count + 1;
        var etapa = Etapa.Crear(Id, titulo, descripcion, orden);
        _etapas.Add(etapa);
        return etapa;
    }

    public void Activar()
    {
        if (Estado == EstadoBusqueda.Activa)
            throw new ExcepcionDominio("La búsqueda del tesoro ya está activa.");
        if (_etapas.Count == 0)
            throw new ExcepcionDominio("La búsqueda del tesoro debe tener al menos una etapa para poder activarse.");

        var etapaSinMisiones = _etapas.FirstOrDefault(e => e.Misiones.Count == 0);
        if (etapaSinMisiones is not null)
            throw new ExcepcionDominio(
                $"La etapa '{etapaSinMisiones.Titulo}' no tiene misiones. Cada etapa debe tener al menos una misión.");

        Estado = EstadoBusqueda.Activa;
        _eventos.Add(new BusquedaActivadaEvento(Id, Nombre, _etapas.Count));
    }

    public void Desactivar()
    {
        if (Estado == EstadoBusqueda.Inactiva)
            throw new ExcepcionDominio("La búsqueda del tesoro ya está inactiva.");

        Estado = EstadoBusqueda.Inactiva;
        _eventos.Add(new BusquedaArchivadaEvento(Id));
    }

    public void ModificarEtapa(Guid etapaId, string nuevoTitulo, string nuevaDescripcion)
    {
        ValidarEstadoInactiva("modificar etapas");

        var etapa = _etapas.FirstOrDefault(e => e.Id == etapaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la etapa con ID '{etapaId}'.");

        etapa.Modificar(nuevoTitulo, nuevaDescripcion);
    }

    public void EliminarEtapa(Guid etapaId)
    {
        ValidarEstadoInactiva("eliminar etapas");

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
        ValidarEstadoInactiva("agregar misiones");

        var etapa = _etapas.FirstOrDefault(e => e.Id == etapaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la etapa con ID '{etapaId}'.");

        return etapa.AgregarMision(titulo, descripcion, tipo, pistaClave);
    }

    public void ModificarMision(Guid etapaId, Guid misionId, string nuevoTitulo, string nuevaDescripcion, TipoMision nuevoTipo, string nuevaPistaClave)
    {
        ValidarEstadoInactiva("modificar misiones");

        var etapa = _etapas.FirstOrDefault(e => e.Id == etapaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la etapa con ID '{etapaId}'.");

        etapa.ModificarMision(misionId, nuevoTitulo, nuevaDescripcion, nuevoTipo, nuevaPistaClave);
    }

    public void EliminarMision(Guid etapaId, Guid misionId)
    {
        ValidarEstadoInactiva("eliminar misiones");

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
        busqueda._etapas.AddRange(etapas);
        return busqueda;
    }

    private void ValidarEstadoInactiva(string accion)
    {
        if (Estado != EstadoBusqueda.Inactiva)
            throw new ExcepcionDominio(
                $"No se pueden {accion} a una búsqueda que está activa.");
    }
}
