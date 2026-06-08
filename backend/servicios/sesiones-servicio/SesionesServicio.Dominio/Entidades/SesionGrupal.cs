using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Dominio.Entidades;

public sealed class SesionGrupal : Sesion
{
    private readonly List<Equipo> _equipos = new();

    public IReadOnlyList<Equipo> Equipos => _equipos.AsReadOnly();

    public override string TipoSesion => "Grupal";

    private SesionGrupal() { }

    public static SesionGrupal Crear(
        string nombre, string descripcion, DateTime fechaProgramada,
        string codigoAcceso, Guid operadorCreadorId, DateTime fechaCreacionUtc)
    {
        var sesion = new SesionGrupal();
        sesion.InicializarBase(nombre, descripcion, fechaProgramada,
            codigoAcceso, operadorCreadorId, fechaCreacionUtc);
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
        if (_equipos.Count >= PoliticaCapacidadSesion.MaximoEquiposPorSesion)
            throw new EquipoInvalidoExcepcion(
                $"La sesión ya alcanzó el máximo de {PoliticaCapacidadSesion.MaximoEquiposPorSesion} equipos.");

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

        // Generamos el id del equipo primero para que el Participante
        // líder pueda nacer con su EquipoId asignado y la invariante
        // "el líder pertenece al equipo" quede protegida desde el inicio.
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

        if (equipo.EstaLleno())
            throw new EquipoInvalidoExcepcion(
                $"El equipo ya tiene {PoliticaCapacidadSesion.MaximoParticipantesPorEquipo} integrantes.");

        var participante = Participante.CrearParaEquipo(
            Id, equipoId, participanteIdentidadId, fechaUnionSesionUtc, fechaUnionEquipoUtc);
        equipo.AgregarParticipante(participante);
        return participante;
    }

    public static SesionGrupal Rehidratar(
        Guid id, string nombre, string descripcion, EstadoSesion estado,
        DateTime fechaProgramada, string codigoAcceso,
        Guid operadorCreadorId, DateTime fechaCreacion,
        DateTime? fechaInicioUtc, DateTime? fechaFinalizacionUtc,
        IEnumerable<SesionMision>? misiones = null,
        IEnumerable<Equipo>? equipos = null)
    {
        var sesion = new SesionGrupal();
        sesion.EstablecerDatosBase(
            id, nombre, descripcion, estado,
            fechaProgramada, codigoAcceso,
            operadorCreadorId, fechaCreacion,
            fechaInicioUtc, fechaFinalizacionUtc, misiones);
        if (equipos is not null) sesion._equipos.AddRange(equipos);
        return sesion;
    }
}
