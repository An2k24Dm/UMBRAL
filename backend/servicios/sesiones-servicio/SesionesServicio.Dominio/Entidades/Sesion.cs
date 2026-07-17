using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Estados;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Dominio.Entidades;

public abstract class Sesion : ISesion
{
    private readonly List<SesionMision> _misiones = new();
    private readonly List<EjecucionActualSesion> _secuenciaEtapas = new();
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
    public int? DuracionSegundosLimite { get; protected set; }
    public EjecucionActualSesion? EjecucionActual { get; protected set; }
    public abstract string TipoSesion { get; }
    public IReadOnlyList<SesionMision> Misiones => _misiones.AsReadOnly();
    public IReadOnlyList<EjecucionActualSesion> SecuenciaEtapas => _secuenciaEtapas.AsReadOnly();
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

    public void AplicarDuracion(int? duracionSegundosLimite)
    {
        GarantizarModificable();
        DuracionSegundosLimite = duracionSegundosLimite;
    }

    protected void GarantizarModificable()
    {
        if (Estado != EstadoSesion.Programada)
            throw new SesionNoModificableExcepcion(
                "Solo se pueden modificar sesiones en estado Programada.");
    }

    public void ValidarPuedeEliminarse()
    {
        if (Estado != EstadoSesion.Programada)
            throw new SesionNoEliminableExcepcion(
                "Solo se pueden eliminar sesiones en estado Programada.");
    }

    protected void ValidarPuedeExpulsar()
    {
        if (Estado is EstadoSesion.EnPreparacion or EstadoSesion.Pausada)
            return;

        if (Estado == EstadoSesion.Activa)
            throw new ExpulsionNoPermitidaExcepcion(
                "Debes pausar la sesión antes de expulsar participantes o equipos.");

        if (Estado == EstadoSesion.Finalizada)
            throw new ExpulsionNoPermitidaExcepcion(
                "No se pueden expulsar participantes o equipos de una sesión finalizada.");

        throw new ExpulsionNoPermitidaExcepcion(
            "Solo se pueden expulsar participantes o equipos cuando la sesión " +
            "está En Preparación o Pausada.");
    }

    public void ValidarPuedePenalizar()
    {
        if (Estado is EstadoSesion.Activa or EstadoSesion.Pausada)
            return;

        throw new PenalizacionNoPermitidaExcepcion(
            "Solo se puede aplicar una penalización cuando la sesión está Activa o Pausada.");
    }

    public void Preparar() => _estadoActual.Preparar(this);

    public void Iniciar(DateTime fechaInicioUtc)
    {
        _estadoActual.Iniciar(this);
        FechaInicioUtc = fechaInicioUtc;
    }

    public void EstablecerSecuenciaEtapas(IEnumerable<EjecucionActualSesion> etapas)
    {
        if (etapas is null)
            throw new SesionInvalidaExcepcion("La secuencia de etapas es obligatoria.");

        var lista = etapas.ToList();
        if (lista.Count == 0)
            throw new MisionSinEtapasExcepcion(
                "La sesión no tiene etapas jugables para iniciar.");
        if (lista.Any(e => !e.EstaPlanificada))
            throw new SesionInvalidaExcepcion(
                "Todas las etapas del plan deben estar en fase Planificada.");
        if (lista.Select(e => e.OrdenGlobal).Distinct().Count() != lista.Count)
            throw new SesionInvalidaExcepcion(
                "El orden global de las etapas del plan no puede repetirse.");
        if (lista.Select(e => e.EtapaId).Distinct().Count() != lista.Count)
            throw new SesionInvalidaExcepcion(
                "No se pueden repetir etapas dentro del plan de la sesión.");

        _secuenciaEtapas.Clear();
        _secuenciaEtapas.AddRange(lista.OrderBy(e => e.OrdenGlobal));
    }

    public void IniciarPrimeraEtapa(EjecucionActualSesion primeraEtapa, DateTime ahoraUtc)
    {
        if (primeraEtapa is null)
            throw new SesionInvalidaExcepcion("La primera etapa es obligatoria.");
        if (!primeraEtapa.EstaPlanificada)
            throw new SesionInvalidaExcepcion(
                "La primera etapa debe estar en fase Planificada.");

        Iniciar(ahoraUtc);
        EjecucionActual = primeraEtapa.Iniciar(ahoraUtc);
    }

    public void IniciarPrimeraEtapa(
        Guid misionId,
        Guid etapaId,
        Guid modoDeJuegoId,
        string tipoEtapa,
        int ordenGlobal,
        DateTime fechaInicioUtc,
        int duracionSegundos)
        => IniciarPrimeraEtapa(
            EjecucionActualSesion.Planificar(
                misionId, etapaId, modoDeJuegoId, tipoEtapa, ordenGlobal, 1, 1, duracionSegundos),
            fechaInicioUtc);

    public void AvanzarASiguienteEtapa(
        Guid etapaActualId,
        Guid siguienteMisionId,
        Guid siguienteEtapaId,
        Guid siguienteModoDeJuegoId,
        string siguienteTipoEtapa,
        int siguienteOrdenGlobal,
        DateTime fechaInicioUtc,
        int duracionSegundos)
    {
        if (Estado != EstadoSesion.Activa)
            throw new TransicionEstadoSesionInvalidaExcepcion(
                Estado,
                "avanzar de etapa",
                "Solo se puede avanzar de etapa cuando la sesion esta activa.");
        if (EjecucionActual is null)
            throw new SesionInvalidaExcepcion("No existe una etapa global activa.");
        if (EjecucionActual.EtapaId != etapaActualId)
            throw new SesionInvalidaExcepcion("La etapa indicada no es la etapa global activa.");

        EstablecerEjecucionActual(
            siguienteMisionId,
            siguienteEtapaId,
            siguienteModoDeJuegoId,
            siguienteTipoEtapa,
            siguienteOrdenGlobal,
            fechaInicioUtc,
            duracionSegundos);
    }

    public void CompletarUltimaEtapa(Guid etapaActualId)
    {
        if (EjecucionActual is null)
            throw new SesionInvalidaExcepcion("No existe una etapa global activa.");
        if (EjecucionActual.EtapaId != etapaActualId)
            throw new SesionInvalidaExcepcion("La etapa indicada no es la etapa global activa.");

        EjecucionActual = null;
    }

    public void ProgramarSiguienteEtapa(
        Guid etapaActualId,
        EjecucionActualSesion siguiente,
        DateTime fechaInicioPreparacionUtc,
        int duracionPreparacionSegundos)
    {
        if (Estado != EstadoSesion.Activa)
            throw new TransicionEstadoSesionInvalidaExcepcion(
                Estado,
                "programar la siguiente etapa",
                "Solo se puede programar la siguiente etapa cuando la sesion esta activa.");
        if (EjecucionActual is null)
            throw new SesionInvalidaExcepcion("No existe una etapa global activa.");
        if (EjecucionActual.EtapaId != etapaActualId)
            throw new SesionInvalidaExcepcion("La etapa indicada no es la etapa global activa.");
        if (siguiente is null)
            throw new SesionInvalidaExcepcion("La siguiente etapa es obligatoria.");
        if (!siguiente.EstaPlanificada)
            throw new SesionInvalidaExcepcion(
                "La siguiente etapa debe provenir del plan (fase Planificada).");

        EjecucionActual = siguiente.Programar(fechaInicioPreparacionUtc, duracionPreparacionSegundos);
    }

    public void ActivarEtapaProgramada(Guid etapaId, DateTime ahoraUtc)
    {
        if (Estado != EstadoSesion.Activa)
            throw new TransicionEstadoSesionInvalidaExcepcion(
                Estado,
                "activar la etapa programada",
                "Solo se puede activar una etapa cuando la sesion esta activa.");
        if (EjecucionActual is null)
            throw new SesionInvalidaExcepcion("No existe una etapa global activa.");
        if (EjecucionActual.EtapaId != etapaId)
            throw new SesionInvalidaExcepcion("La etapa indicada no es la etapa global activa.");

        EjecucionActual = EjecucionActual.Activar(ahoraUtc);
    }

    public void ProgramarCierrePendiente(
        Guid etapaId, DateTime ahoraUtc, int duracionFeedbackSegundos)
    {
        if (Estado != EstadoSesion.Activa)
            throw new TransicionEstadoSesionInvalidaExcepcion(
                Estado,
                "programar el cierre pendiente",
                "Solo se puede programar el cierre cuando la sesion esta activa.");
        if (EjecucionActual is null)
            throw new SesionInvalidaExcepcion("No existe una etapa global activa.");
        if (EjecucionActual.EtapaId != etapaId)
            throw new SesionInvalidaExcepcion("La etapa indicada no es la etapa global activa.");

        EjecucionActual = EjecucionActual.ProgramarCierrePendiente(ahoraUtc, duracionFeedbackSegundos);
    }

    public void Pausar(DateTime ahoraUtc)
    {
        _estadoActual.Pausar(this);
        EjecucionActual = EjecucionActual?.Pausar(ahoraUtc);
    }

    public void Pausar() => Pausar(DateTime.UtcNow);

    public void Reanudar(DateTime ahoraUtc)
    {
        _estadoActual.Reanudar(this);
        EjecucionActual = EjecucionActual?.Reanudar(ahoraUtc);
    }

    public void Reanudar() => Reanudar(DateTime.UtcNow);

    public void Finalizar(DateTime fechaFinalizacionUtc)
    {
        _estadoActual.Finalizar(this);
        FechaFinalizacionUtc = fechaFinalizacionUtc;
        EjecucionActual = null;
    }

    public void Cancelar()
    {
        _estadoActual.Cancelar(this);
        EjecucionActual = null;
    }

    protected void EstablecerDatosBase(
        Guid id,
        string nombre, string descripcion,
        EstadoSesion estado,
        DateTime fechaProgramada, string codigoAcceso,
        Guid operadorCreadorId, DateTime fechaCreacion,
        DateTime? fechaInicioUtc, DateTime? fechaFinalizacionUtc,
        IEnumerable<SesionMision>? misiones,
        int? duracionSegundosLimite = null,
        EjecucionActualSesion? ejecucionActual = null,
        IEnumerable<EjecucionActualSesion>? secuenciaEtapas = null)
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
        DuracionSegundosLimite = duracionSegundosLimite;
        EjecucionActual = ejecucionActual;
        _estadoActual = FabricaEstadoSesion.Crear(estado);
        _misiones.Clear();
        if (misiones is not null) _misiones.AddRange(misiones);
        _secuenciaEtapas.Clear();
        if (secuenciaEtapas is not null)
            _secuenciaEtapas.AddRange(secuenciaEtapas.OrderBy(e => e.OrdenGlobal));
    }

    internal void CambiarEstado(IEstadoSesion nuevoEstado)
    {
        _estadoActual = nuevoEstado;
        Estado = nuevoEstado.Estado;
    }

    private void EstablecerEjecucionActual(
        Guid misionId,
        Guid etapaId,
        Guid modoDeJuegoId,
        string tipoEtapa,
        int ordenGlobal,
        DateTime fechaInicioUtc,
        int duracionSegundos)
    {
        EjecucionActual = EjecucionActualSesion.Crear(
            misionId,
            etapaId,
            modoDeJuegoId,
            tipoEtapa,
            ordenGlobal,
            fechaInicioUtc,
            duracionSegundos);
    }
}
