using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;
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
        int maximoEquipos, int maximoParticipantesPorEquipo, int? duracionMinutosLimite = null)
    {
        PoliticaCapacidadSesion.ValidarCapacidadGrupal(
            maximoEquipos, maximoParticipantesPorEquipo);

        var sesion = new SesionGrupal();
        sesion.InicializarBase(nombre, descripcion, fechaProgramada,
            codigoAcceso, operadorCreadorId, fechaCreacionUtc);
        sesion.MaximoEquipos = maximoEquipos;
        sesion.MaximoParticipantesPorEquipo = maximoParticipantesPorEquipo;
        sesion.DuracionMinutosLimite = duracionMinutosLimite;
        return sesion;
    }

    public Equipo CrearEquipo(
        NombreEquipo nombreEquipo,
        TipoEquipo tipoEquipo,
        ContrasenaEquipoHash? contrasenaHash,
        Guid liderIdentidadId,
        DateTime fechaUnionSesionUtc,
        DateTime fechaUnionEquipoUtc)
    {
        if (liderIdentidadId == Guid.Empty)
            throw new EquipoInvalidoExcepcion("El líder del equipo es obligatorio.");
        if (nombreEquipo is null)
            throw new EquipoInvalidoExcepcion("El nombre del equipo es obligatorio.");
        if (_equipos.Count >= MaximoEquipos)
            throw new EquipoInvalidoExcepcion(
                "La sesión grupal alcanzó el máximo de equipos permitido.");

        if (tipoEquipo == TipoEquipo.Privado && contrasenaHash is null)
            throw new EquipoInvalidoExcepcion(
                "Un equipo privado debe tener una contraseña configurada.");

        if (_equipos.Any(e => e.Nombre.Equals(nombreEquipo)))
            throw new EquipoInvalidoExcepcion(
                "Ya existe un equipo con ese nombre en la sesión.");
        if (_equipos.Any(e => e.ContieneParticipanteIdentidadId(liderIdentidadId)))
            throw new ParticipacionInvalidaExcepcion(
                "El participante ya forma parte de otro equipo de esta sesión.");

        var equipoId = Guid.NewGuid();
        var lider = Participante.CrearParaEquipo(
            Id, equipoId, liderIdentidadId, fechaUnionSesionUtc, fechaUnionEquipoUtc);
        var equipo = Equipo.CrearConLider(
            equipoId, Id, nombreEquipo, tipoEquipo, contrasenaHash,
            MaximoParticipantesPorEquipo, lider, fechaUnionEquipoUtc);
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

        if (equipo.EstaLleno())
            throw new EquipoInvalidoExcepcion(
                "El equipo alcanzó el máximo de participantes permitido.");

        var participante = Participante.CrearParaEquipo(
            Id, equipoId, participanteIdentidadId, fechaUnionSesionUtc, fechaUnionEquipoUtc);
        equipo.AgregarParticipante(participante);
        return participante;
    }

    public void ExpulsarEquipo(Guid equipoId)
    {
        ValidarPuedeExpulsar();

        var equipo = _equipos.FirstOrDefault(e => e.Id == equipoId)
            ?? throw new EquipoNoEncontradoExcepcion(
                "El equipo indicado no pertenece a esta sesión.");

        _equipos.Remove(equipo);
    }

    public Participante ExpulsarParticipanteDeEquipo(
        Guid equipoId,
        Guid participanteSesionId,
        Guid actorParticipanteIdentidadId,
        bool actorEsOperador)
    {
        if (Estado is not (EstadoSesion.EnPreparacion or EstadoSesion.Pausada))
            throw new ExpulsionNoPermitidaExcepcion(
                "Solo se pueden expulsar participantes de un equipo cuando la " +
                "sesión está En Preparación o Pausada.");

        var equipo = _equipos.FirstOrDefault(e => e.Id == equipoId)
            ?? throw new EquipoNoEncontradoExcepcion(
                "El equipo solicitado no existe en esta sesión.");

        if (!equipo.Participantes.Any(p => p.Id == participanteSesionId))
            throw new ParticipanteNoEncontradoExcepcion(
                "El participante indicado no pertenece a este equipo.");

        if (!actorEsOperador && !equipo.EsLider(actorParticipanteIdentidadId))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo el líder del equipo puede expulsar participantes.");

        // El líder participante no puede expulsar al líder (ni a sí mismo);
        // solo el Operador puede, y el equipo reasigna el liderazgo.
        return equipo.ExpulsarParticipante(
            participanteSesionId, permitirExpulsarLider: actorEsOperador);
    }

    // HU48 — Abandono voluntario del equipo (solo En Preparación, a
    // diferencia de la expulsión HU45 que también permite Pausada). Si el
    // equipo queda vacío se elimina de la sesión, liberando el cupo de
    // equipos. El participante queda fuera de la sesión grupal y puede crear
    // o unirse a otro equipo mientras la sesión siga En Preparación.
    public Participante AbandonarEquipo(Guid participanteIdentidadId)
    {
        if (Estado != EstadoSesion.EnPreparacion)
            throw new ParticipacionInvalidaExcepcion(
                "Solo puedes abandonar un equipo cuando la sesión está en " +
                "estado En Preparación.");

        var equipo = _equipos.FirstOrDefault(
                e => e.ContieneParticipanteIdentidadId(participanteIdentidadId))
            ?? throw new ParticipanteNoEncontradoExcepcion(
                "El participante no pertenece a ningún equipo de esta sesión.");

        var participanteRemovido = equipo.AbandonarParticipante(participanteIdentidadId);

        if (equipo.Participantes.Count == 0)
            _equipos.Remove(equipo);

        return participanteRemovido;
    }

    public Equipo ModificarEquipo(
        Guid equipoId,
        Guid participanteIdentidadId,
        NombreEquipo nuevoNombre,
        TipoEquipo nuevoTipo,
        ContrasenaEquipoHash? nuevaContrasenaHash,
        bool actualizarContrasena)
    {
        if (nuevoNombre is null)
            throw new EquipoInvalidoExcepcion("El nombre del equipo es obligatorio.");

        var equipo = _equipos.FirstOrDefault(e => e.Id == equipoId)
            ?? throw new EquipoNoEncontradoExcepcion(
                "El equipo solicitado no existe en esta sesión.");

        if (Estado != EstadoSesion.EnPreparacion)
            throw new EquipoInvalidoExcepcion(
                "Solo se pueden modificar equipos cuando la sesión está en estado En Preparación.");

        if (!equipo.EsLider(participanteIdentidadId))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo el líder del equipo puede modificarlo.");

        if (_equipos.Any(e => e.Id != equipoId && e.Nombre.Equals(nuevoNombre)))
            throw new EquipoInvalidoExcepcion(
                "Ya existe un equipo con ese nombre en la sesión.");

        equipo.ModificarDatos(nuevoNombre, nuevoTipo, nuevaContrasenaHash, actualizarContrasena);
        return equipo;
    }

    public void EliminarEquipo(Guid equipoId, Guid participanteIdentidadId)
    {
        var equipo = _equipos.FirstOrDefault(e => e.Id == equipoId)
            ?? throw new EquipoNoEncontradoExcepcion(
                "El equipo solicitado no existe en esta sesión.");

        if (Estado != EstadoSesion.EnPreparacion)
            throw new EquipoInvalidoExcepcion(
                "Solo se pueden eliminar equipos cuando la sesión está en estado En Preparación.");

        if (!equipo.EsLider(participanteIdentidadId))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo el líder del equipo puede eliminarlo.");

        _equipos.Remove(equipo);
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
        IEnumerable<Equipo>? equipos = null,
        int? duracionMinutosLimite = null)
    {
        var sesion = new SesionGrupal();
        sesion.EstablecerDatosBase(
            id, nombre, descripcion, estado,
            fechaProgramada, codigoAcceso,
            operadorCreadorId, fechaCreacion,
            fechaInicioUtc, fechaFinalizacionUtc, misiones, duracionMinutosLimite);
        sesion.MaximoEquipos = maximoEquipos;
        sesion.MaximoParticipantesPorEquipo = maximoParticipantesPorEquipo;
        if (equipos is not null) sesion._equipos.AddRange(equipos);
        return sesion;
    }
}
