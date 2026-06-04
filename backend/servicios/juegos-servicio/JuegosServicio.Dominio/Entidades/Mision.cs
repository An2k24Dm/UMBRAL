using JuegosServicio.Dominio.Abstract;
using JuegosServicio.Dominio.Dificultades;
using JuegosServicio.Dominio.Enums;
using JuegosServicio.Dominio.Excepciones;

namespace JuegosServicio.Dominio.Entidades;

// Aggregate root: secuencia ordenada de etapas (modos de juego) que
// se asigna a una sesión para guiar a los participantes.
public sealed class Mision
{
    private readonly List<Etapa> _etapas = new();
    private IDificultadMision _dificultad = default!;

    public Guid Id { get; private set; }
    public string Nombre { get; private set; } = default!;
    public string Descripcion { get; private set; } = default!;
    public Guid CreadorId { get; private set; }
    public EstadoMision Estado { get; private set; }
    public NivelDificultad Dificultad { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    public IDificultadMision ObtenerDificultad() => _dificultad;

    public IReadOnlyList<Etapa> Etapas => _etapas.AsReadOnly();

    private Mision() { }

    public static Mision Crear(
        string nombre,
        string descripcion,
        Guid creadorId,
        DateTime fechaCreacion,
        NivelDificultad dificultad = NivelDificultad.Media)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ExcepcionDominio("El nombre de la misión es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la misión es obligatoria.");
        if (creadorId == Guid.Empty)
            throw new ExcepcionDominio("El identificador del creador es obligatorio.");

        var mision = new Mision
        {
            Id = Guid.NewGuid(),
            Nombre = nombre.Trim(),
            Descripcion = descripcion.Trim(),
            CreadorId = creadorId,
            Estado = EstadoMision.Inactiva,
            Dificultad = dificultad,
            FechaCreacion = fechaCreacion
        };
        mision._dificultad = FabricaDificultadMision.Obtener(dificultad);
        return mision;
    }

    public Etapa AgregarEtapa(TipoModoDeJuego tipo, Guid modoDeJuegoId)
    {
        if (Estado == EstadoMision.Activa)
            throw new ExcepcionDominio("No se pueden agregar etapas a una misión activa.");
        if (modoDeJuegoId == Guid.Empty)
            throw new ExcepcionDominio("El identificador del modo de juego es obligatorio.");

        var orden = _etapas.Count + 1;
        var etapa = Etapa.Crear(Id, orden, tipo, modoDeJuegoId);
        _etapas.Add(etapa);
        return etapa;
    }

    public void EliminarEtapa(Guid etapaId)
    {
        if (Estado == EstadoMision.Activa)
            throw new ExcepcionDominio("No se pueden eliminar etapas de una misión activa.");

        var etapa = _etapas.FirstOrDefault(e => e.Id == etapaId)
            ?? throw new ExcepcionNoEncontrado($"No se encontró la etapa con ID '{etapaId}'.");

        _etapas.Remove(etapa);
        RenumerarEtapas();
    }

    public void Modificar(string nombre, string descripcion, NivelDificultad dificultad)
    {
        if (Estado == EstadoMision.Activa)
            throw new ExcepcionDominio("No se puede modificar una misión activa.");
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ExcepcionDominio("El nombre de la misión es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new ExcepcionDominio("La descripción de la misión es obligatoria.");
        Nombre = nombre.Trim();
        Descripcion = descripcion.Trim();
        Dificultad = dificultad;
        _dificultad = FabricaDificultadMision.Obtener(dificultad);
    }

    public void Activar()
    {
        if (Estado == EstadoMision.Activa)
            throw new ExcepcionDominio("La misión ya está activa.");
        if (_etapas.Count == 0)
            throw new ExcepcionDominio("La misión debe tener al menos una etapa para poder activarse.");
        Estado = EstadoMision.Activa;
    }

    public void Desactivar()
    {
        if (Estado == EstadoMision.Inactiva)
            throw new ExcepcionDominio("La misión ya está inactiva.");
        Estado = EstadoMision.Inactiva;
    }

    public static Mision Reconstituir(
        Guid id,
        string nombre,
        string descripcion,
        Guid creadorId,
        EstadoMision estado,
        NivelDificultad dificultad,
        DateTime fechaCreacion,
        IEnumerable<Etapa>? etapas = null)
    {
        var mision = new Mision
        {
            Id = id,
            Nombre = nombre,
            Descripcion = descripcion,
            CreadorId = creadorId,
            Estado = estado,
            Dificultad = dificultad,
            FechaCreacion = fechaCreacion
        };
        mision._dificultad = FabricaDificultadMision.Obtener(dificultad);
        if (etapas is not null) mision._etapas.AddRange(etapas);
        return mision;
    }

    private void RenumerarEtapas()
    {
        for (int i = 0; i < _etapas.Count; i++)
            _etapas[i].ActualizarOrden(i + 1);
    }
}
