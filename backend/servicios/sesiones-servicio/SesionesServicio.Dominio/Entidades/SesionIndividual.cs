using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Dominio.Entidades;

public sealed class SesionIndividual : Sesion
{
    private readonly List<Participante> _participantes = new();

    public IReadOnlyList<Participante> Participantes => _participantes.AsReadOnly();

    public override string TipoSesion => "Individual";

    private SesionIndividual() { }

    public static SesionIndividual Crear(
        string nombre, string descripcion, DateTime fechaProgramada,
        string codigoAcceso, Guid operadorCreadorId, DateTime fechaCreacionUtc)
    {
        var sesion = new SesionIndividual();
        sesion.InicializarBase(nombre, descripcion, fechaProgramada,
            codigoAcceso, operadorCreadorId, fechaCreacionUtc);
        return sesion;
    }

    public Participante AgregarParticipante(
        Guid participanteIdentidadId, DateTime fechaUnionSesionUtc)
    {
        if (participanteIdentidadId == Guid.Empty)
            throw new ParticipacionInvalidaExcepcion(
                "El identificador del participante es obligatorio.");
        if (_participantes.Count >= PoliticaCapacidadSesion.MaximoParticipantesIndividual)
            throw new ParticipacionInvalidaExcepcion(
                $"La sesión ya alcanzó el máximo de {PoliticaCapacidadSesion.MaximoParticipantesIndividual} participantes.");
        if (_participantes.Any(p => p.ParticipanteIdentidadId == participanteIdentidadId))
            throw new ParticipacionInvalidaExcepcion(
                "El participante ya forma parte de esta sesión.");

        var participante = Participante.CrearParaSesionIndividual(
            Id, participanteIdentidadId, fechaUnionSesionUtc);
        _participantes.Add(participante);
        return participante;
    }

    public static SesionIndividual Rehidratar(
        Guid id, string nombre, string descripcion, EstadoSesion estado,
        DateTime fechaProgramada, string codigoAcceso,
        Guid operadorCreadorId, DateTime fechaCreacion,
        DateTime? fechaInicioUtc, DateTime? fechaFinalizacionUtc,
        IEnumerable<SesionMision>? misiones = null,
        IEnumerable<Participante>? participantes = null)
    {
        var sesion = new SesionIndividual();
        sesion.EstablecerDatosBase(
            id, nombre, descripcion, estado,
            fechaProgramada, codigoAcceso,
            operadorCreadorId, fechaCreacion,
            fechaInicioUtc, fechaFinalizacionUtc, misiones);
        if (participantes is not null) sesion._participantes.AddRange(participantes);
        return sesion;
    }
}
