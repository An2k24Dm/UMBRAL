using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Dominio.Entidades;

public sealed class SesionGrupal : Sesion
{
    private readonly List<Equipo> _equipos = new();
    public IReadOnlyList<Equipo> Equipos => _equipos.AsReadOnly();
    public int MaximoEquipos { get; private set; }
    public int MaximoParticipantesPorEquipo { get; private set; }
    public override string TipoSesion => "Grupal";
    public override bool TieneInscritos => _equipos.Count > 0;

    private SesionGrupal() { }

    public static SesionGrupal Crear(
        string nombre, string descripcion, DateTime fechaProgramada,
        string codigoAcceso, Guid operadorCreadorId, DateTime fechaCreacionUtc,
        int maximoEquipos, int maximoParticipantesPorEquipo)
    {
        PoliticaCapacidadSesion.ValidarCapacidadGrupal(
            maximoEquipos, maximoParticipantesPorEquipo);

        var sesion = new SesionGrupal();
        sesion.InicializarBase(nombre, descripcion, fechaProgramada,
            codigoAcceso, operadorCreadorId, fechaCreacionUtc);
        sesion.MaximoEquipos = maximoEquipos;
        sesion.MaximoParticipantesPorEquipo = maximoParticipantesPorEquipo;
        return sesion;
    }

    public Equipo CrearEquipo(
        string nombreEquipo,
        Guid liderIdentidadId,
        DateTime fechaUnionSesionUtc,
        DateTime fechaUnionEquipoUtc)
    {
        if (liderIdentidadId == Guid.Empty)
            throw new EquipoInvalidoExcepcion("El líder del equipo es obligatorio.");
        if (_equipos.Count >= MaximoEquipos)
            throw new EquipoInvalidoExcepcion(
                "La sesión grupal alcanzó el máximo de equipos permitido.");

        var nombreNormalizado = (nombreEquipo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombreNormalizado))
            throw new EquipoInvalidoExcepcion("El nombre del equipo es obligatorio.");
        if (_equipos.Any(e => string.Equals(e.Nombre, nombreNormalizado,
                StringComparison.OrdinalIgnoreCase)))
            throw new EquipoInvalidoExcepcion(
                "Ya existe un equipo con ese nombre en la sesión.");
        if (_equipos.Any(e => e.ContieneParticipanteIdentidadId(liderIdentidadId)))
            throw new ParticipacionInvalidaExcepcion(
                "El participante ya forma parte de otro equipo de esta sesión.");

        var equipoId = Guid.NewGuid();
        var lider = Participante.CrearParaEquipo(
            Id, equipoId, liderIdentidadId, fechaUnionSesionUtc, fechaUnionEquipoUtc);
        var equipo = Equipo.CrearConLider(equipoId, Id, nombreNormalizado, lider, fechaUnionEquipoUtc);
        _equipos.Add(equipo);
        return equipo;
    }

    public Participante AgregarParticipanteAEquipo(
        Guid equipoId,
        Guid participanteIdentidadId,
        DateTime fechaUnionSesionUtc,
        DateTime fechaUnionEquipoUtc)
    {
        if (participanteIdentidadId == Guid.Empty)
            throw new ParticipacionInvalidaExcepcion(
                "El identificador del participante es obligatorio.");

        var equipo = _equipos.FirstOrDefault(e => e.Id == equipoId)
            ?? throw new EquipoInvalidoExcepcion(
                "El equipo indicado no pertenece a esta sesión.");

        if (_equipos.Any(e => e.ContieneParticipanteIdentidadId(participanteIdentidadId)))
            throw new ParticipacionInvalidaExcepcion(
                "El participante ya forma parte de un equipo de esta sesión.");

        if (equipo.EstaLleno(MaximoParticipantesPorEquipo))
            throw new EquipoInvalidoExcepcion(
                "El equipo alcanzó el máximo de participantes permitido.");

        var participante = Participante.CrearParaEquipo(
            Id, equipoId, participanteIdentidadId, fechaUnionSesionUtc, fechaUnionEquipoUtc);
        equipo.AgregarParticipante(participante, MaximoParticipantesPorEquipo);
        return participante;
    }

    public void ModificarCapacidad(int maximoEquipos, int maximoParticipantesPorEquipo)
    {
        GarantizarModificable();
        PoliticaCapacidadSesion.ValidarCapacidadGrupal(maximoEquipos, maximoParticipantesPorEquipo);
        if (_equipos.Count > maximoEquipos)
            throw new SesionInvalidaExcepcion(
                "No se puede establecer una capacidad menor a la cantidad de equipos actuales.");
        if (_equipos.Any(e => e.Participantes.Count > maximoParticipantesPorEquipo))
            throw new SesionInvalidaExcepcion(
                "No se puede establecer una capacidad por equipo menor a la cantidad de integrantes actuales.");
        MaximoEquipos = maximoEquipos;
        MaximoParticipantesPorEquipo = maximoParticipantesPorEquipo;
    }

    public override void AplicarCapacidad(
        int? maximoParticipantes, int? maximoEquipos, int? maximoParticipantesPorEquipo)
    {
        var equipos = maximoEquipos
            ?? throw new SesionInvalidaExcepcion(
                "Debe indicar el máximo de equipos para una sesión grupal.");
        var porEquipo = maximoParticipantesPorEquipo
            ?? throw new SesionInvalidaExcepcion(
                "Debe indicar el máximo de participantes por equipo para una sesión grupal.");
        ModificarCapacidad(equipos, porEquipo);
    }

    public static SesionGrupal Rehidratar(
        Guid id, string nombre, string descripcion, EstadoSesion estado,
        DateTime fechaProgramada, string codigoAcceso,
        Guid operadorCreadorId, DateTime fechaCreacion,
        DateTime? fechaInicioUtc, DateTime? fechaFinalizacionUtc,
        int maximoEquipos, int maximoParticipantesPorEquipo,
        IEnumerable<SesionMision>? misiones = null,
        IEnumerable<Equipo>? equipos = null)
    {
        var sesion = new SesionGrupal();
        sesion.EstablecerDatosBase(
            id, nombre, descripcion, estado,
            fechaProgramada, codigoAcceso,
            operadorCreadorId, fechaCreacion,
            fechaInicioUtc, fechaFinalizacionUtc, misiones);
        sesion.MaximoEquipos = maximoEquipos;
        sesion.MaximoParticipantesPorEquipo = maximoParticipantesPorEquipo;
        if (equipos is not null) sesion._equipos.AddRange(equipos);
        return sesion;
    }
}
