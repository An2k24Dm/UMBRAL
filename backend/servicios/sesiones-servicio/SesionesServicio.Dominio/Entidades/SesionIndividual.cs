using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.Politicas;

namespace SesionesServicio.Dominio.Entidades;

public sealed class SesionIndividual : Sesion
{
    private readonly List<Participante> _participantes = new();
    public IReadOnlyList<Participante> Participantes => _participantes.AsReadOnly();
    public int MaximoParticipantes { get; private set; }
    public override string TipoSesion => "Individual";
    public override bool TieneInscritos => _participantes.Count > 0;

    private SesionIndividual() { }

    public static SesionIndividual Crear(
        string nombre, string descripcion, DateTime fechaProgramada,
        string codigoAcceso, Guid operadorCreadorId, DateTime fechaCreacionUtc,
        int maximoParticipantes)
    {
        PoliticaCapacidadSesion.ValidarCapacidadIndividual(maximoParticipantes);

        var sesion = new SesionIndividual();
        sesion.InicializarBase(nombre, descripcion, fechaProgramada,
            codigoAcceso, operadorCreadorId, fechaCreacionUtc);
        sesion.MaximoParticipantes = maximoParticipantes;
        return sesion;
    }

    public Participante AgregarParticipante(
        Guid participanteIdentidadId, DateTime fechaUnionSesionUtc)
    {
        if (participanteIdentidadId == Guid.Empty)
            throw new ParticipacionInvalidaExcepcion(
                "El identificador del participante es obligatorio.");
        if (_participantes.Count >= MaximoParticipantes)
            throw new ParticipacionInvalidaExcepcion(
                "La sesión individual alcanzó el máximo de participantes permitido.");
        if (_participantes.Any(p => p.ParticipanteIdentidadId == participanteIdentidadId))
            throw new ParticipacionInvalidaExcepcion(
                "El participante ya forma parte de esta sesión.");

        var participante = Participante.CrearParaSesionIndividual(
            Id, participanteIdentidadId, fechaUnionSesionUtc);
        _participantes.Add(participante);
        return participante;
    }

    public void ModificarCapacidad(int maximoParticipantes)
    {
        GarantizarModificable();
        PoliticaCapacidadSesion.ValidarCapacidadIndividual(maximoParticipantes);
        if (_participantes.Count > maximoParticipantes)
            throw new SesionInvalidaExcepcion(
                "No se puede establecer una capacidad menor a la cantidad de participantes actuales.");
        MaximoParticipantes = maximoParticipantes;
    }

    public override void AplicarCapacidad(
        int? maximoParticipantes, int? maximoEquipos, int? maximoParticipantesPorEquipo)
    {
        var maximo = maximoParticipantes
            ?? throw new SesionInvalidaExcepcion(
                "Debe indicar el máximo de participantes para una sesión individual.");
        ModificarCapacidad(maximo);
    }

    public static SesionIndividual Rehidratar(
        Guid id, string nombre, string descripcion, EstadoSesion estado,
        DateTime fechaProgramada, string codigoAcceso,
        Guid operadorCreadorId, DateTime fechaCreacion,
        DateTime? fechaInicioUtc, DateTime? fechaFinalizacionUtc,
        int maximoParticipantes,
        IEnumerable<SesionMision>? misiones = null,
        IEnumerable<Participante>? participantes = null)
    {
        var sesion = new SesionIndividual();
        sesion.EstablecerDatosBase(
            id, nombre, descripcion, estado,
            fechaProgramada, codigoAcceso,
            operadorCreadorId, fechaCreacion,
            fechaInicioUtc, fechaFinalizacionUtc, misiones);
        sesion.MaximoParticipantes = maximoParticipantes;
        if (participantes is not null) sesion._participantes.AddRange(participantes);
        return sesion;
    }
}
