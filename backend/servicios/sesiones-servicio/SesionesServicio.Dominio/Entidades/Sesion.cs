using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Estados;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Dominio.Entidades;

public abstract class Sesion : ISesion
{
    private readonly List<SesionMision> _misiones = new();
    private IEstadoSesion _estadoActual = null!;
    public Guid Id { get; protected set; }
    public string Nombre { get; protected set; } = string.Empty;
    public string Descripcion { get; protected set; } = string.Empty;
    public EstadoSesion Estado { get; protected set; }
    public DateTime FechaProgramada { get; protected set; }
    public string CodigoAcceso { get; protected set; } = string.Empty;
    public Guid OperadorCreadorId { get; protected set; }
    public DateTime FechaCreacion { get; protected set; }
    public DateTime? FechaInicioUtc { get; protected set; }
    public DateTime? FechaFinalizacionUtc { get; protected set; }
    public abstract string TipoSesion { get; }
    public IReadOnlyList<SesionMision> Misiones => _misiones.AsReadOnly();
    public abstract bool TieneInscritos { get; }

    protected Sesion() { }

    protected void InicializarBase(
        string nombre,
        string descripcion,
        DateTime fechaProgramada,
        string codigoAcceso,
        Guid operadorCreadorId,
        DateTime fechaCreacionUtc)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new SesionInvalidaExcepcion("El nombre de la sesión es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new SesionInvalidaExcepcion("La descripción de la sesión es obligatoria.");
        if (string.IsNullOrWhiteSpace(codigoAcceso))
            throw new SesionInvalidaExcepcion("El código de acceso es obligatorio.");
        if (operadorCreadorId == Guid.Empty)
            throw new SesionInvalidaExcepcion(
                "El identificador del operador creador es obligatorio.");

        Id = Guid.NewGuid();
        Nombre = nombre.Trim();
        Descripcion = descripcion.Trim();
        Estado = EstadoSesion.Programada;
        FechaProgramada = fechaProgramada;
        CodigoAcceso = codigoAcceso.Trim();
        OperadorCreadorId = operadorCreadorId;
        FechaCreacion = fechaCreacionUtc;
        _estadoActual = FabricaEstadoSesion.Crear(EstadoSesion.Programada);
    }

    public void AsignarMisiones(IReadOnlyList<Guid> misionesIds)
    {
        if (misionesIds is null || misionesIds.Count < PoliticaCapacidadSesion.MinimoMisionesPorSesion)
            throw new SesionInvalidaExcepcion(
                $"La sesión debe tener al menos {PoliticaCapacidadSesion.MinimoMisionesPorSesion} misión.");
        if (misionesIds.Count > PoliticaCapacidadSesion.MaximoMisionesPorSesion)
            throw new SesionInvalidaExcepcion(
                $"La sesión no puede tener más de {PoliticaCapacidadSesion.MaximoMisionesPorSesion} misiones.");
        if (misionesIds.Any(id => id == Guid.Empty))
            throw new SesionInvalidaExcepcion("Hay misiones con identificador vacío.");
        if (misionesIds.Distinct().Count() != misionesIds.Count)
            throw new SesionInvalidaExcepcion(
                "No se pueden repetir misiones dentro de la misma sesión.");

        _misiones.Clear();
        var orden = 1;
        foreach (var misionId in misionesIds)
        {
            _misiones.Add(SesionMision.Crear(Id, misionId, orden));
            orden++;
        }
    }

    public void ModificarDatosBasicos(
        string nombre,
        string descripcion,
        DateTime fechaProgramada,
        DateTime ahoraUtc)
    {
        GarantizarModificable();

        if (string.IsNullOrWhiteSpace(nombre))
            throw new SesionInvalidaExcepcion("El nombre de la sesión es obligatorio.");
        if (string.IsNullOrWhiteSpace(descripcion))
            throw new SesionInvalidaExcepcion("La descripción de la sesión es obligatoria.");

        PoliticaProgramacionSesion.ValidarFechaProgramada(fechaProgramada, ahoraUtc);

        Nombre = nombre.Trim();
        Descripcion = descripcion.Trim();
        FechaProgramada = fechaProgramada;
    }

    public void ReemplazarMisiones(IReadOnlyList<Guid> misionesIds)
    {
        GarantizarModificable();
        AsignarMisiones(misionesIds);
    }

    public abstract void AplicarCapacidad(
        int? maximoParticipantes,
        int? maximoEquipos,
        int? maximoParticipantesPorEquipo);

    protected void GarantizarModificable()
    {
        if (Estado != EstadoSesion.Programada)
            throw new SesionNoModificableExcepcion(
                "Solo se pueden modificar sesiones en estado Programada.");
    }

    public void Preparar() => _estadoActual.Preparar(this);

    public void Iniciar(DateTime fechaInicioUtc)
    {
        _estadoActual.Iniciar(this);
        FechaInicioUtc = fechaInicioUtc;
    }

    public void Pausar() => _estadoActual.Pausar(this);

    public void Reanudar() => _estadoActual.Reanudar(this);

    public void Finalizar(DateTime fechaFinalizacionUtc)
    {
        _estadoActual.Finalizar(this);
        FechaFinalizacionUtc = fechaFinalizacionUtc;
    }

    public void Cancelar() => _estadoActual.Cancelar(this);

    protected void EstablecerDatosBase(
        Guid id,
        string nombre, string descripcion,
        EstadoSesion estado,
        DateTime fechaProgramada, string codigoAcceso,
        Guid operadorCreadorId, DateTime fechaCreacion,
        DateTime? fechaInicioUtc, DateTime? fechaFinalizacionUtc,
        IEnumerable<SesionMision>? misiones)
    {
        Id = id;
        Nombre = nombre;
        Descripcion = descripcion;
        Estado = estado;
        FechaProgramada = fechaProgramada;
        CodigoAcceso = codigoAcceso;
        OperadorCreadorId = operadorCreadorId;
        FechaCreacion = fechaCreacion;
        FechaInicioUtc = fechaInicioUtc;
        FechaFinalizacionUtc = fechaFinalizacionUtc;
        _estadoActual = FabricaEstadoSesion.Crear(estado);
        _misiones.Clear();
        if (misiones is not null) _misiones.AddRange(misiones);
    }

    internal void CambiarEstado(IEstadoSesion nuevoEstado)
    {
        _estadoActual = nuevoEstado;
        Estado = nuevoEstado.Estado;
    }
}
