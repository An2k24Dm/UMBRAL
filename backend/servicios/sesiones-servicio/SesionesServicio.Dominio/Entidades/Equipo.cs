using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Entidades;

public sealed class Equipo
{
    private readonly List<Participante> _participantes = new();

    public Guid Id { get; private set; }
    public Guid SesionId { get; private set; }
    public string Nombre { get; private set; } = string.Empty;
    public Guid LiderParticipanteId { get; private set; }
    public int Puntaje { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public IReadOnlyList<Participante> Participantes => _participantes.AsReadOnly();

    private Equipo() { }

    internal static Equipo CrearConLider(
        Guid equipoId, Guid sesionId, string nombre,
        Participante lider, DateTime fechaCreacionUtc)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new EquipoInvalidoExcepcion("El nombre del equipo es obligatorio.");
        if (lider is null)
            throw new EquipoInvalidoExcepcion("El líder del equipo es obligatorio.");
        if (lider.EquipoId != equipoId)
            throw new EquipoInvalidoExcepcion(
                "El líder debe pertenecer al equipo que se está creando.");

        var equipo = new Equipo
        {
            Id = equipoId,
            SesionId = sesionId,
            Nombre = nombre.Trim(),
            LiderParticipanteId = lider.Id,
            Puntaje = 0,
            FechaCreacion = fechaCreacionUtc
        };
        equipo._participantes.Add(lider);
        return equipo;
    }

    internal void AgregarParticipante(Participante participante, int maximoParticipantes)
    {
        if (participante is null)
            throw new EquipoInvalidoExcepcion("El participante es obligatorio.");
        if (participante.EquipoId != Id)
            throw new EquipoInvalidoExcepcion(
                "El participante no pertenece a este equipo.");
        if (_participantes.Count >= maximoParticipantes)
            throw new EquipoInvalidoExcepcion(
                "El equipo alcanzó el máximo de participantes permitido.");
        if (_participantes.Any(p => p.ParticipanteIdentidadId == participante.ParticipanteIdentidadId))
            throw new ParticipacionInvalidaExcepcion(
                "El participante ya forma parte de este equipo.");

        _participantes.Add(participante);
    }

    public bool ContieneParticipanteIdentidadId(Guid participanteIdentidadId)
        => _participantes.Any(p => p.ParticipanteIdentidadId == participanteIdentidadId);

    public bool EstaLleno(int maximoParticipantes)
        => _participantes.Count >= maximoParticipantes;

    public void SumarPuntaje(int puntos)
    {
        if (puntos < 0)
            throw new EquipoInvalidoExcepcion("El puntaje a sumar no puede ser negativo.");
        Puntaje += puntos;
    }

    public static Equipo Rehidratar(
        Guid id, Guid sesionId, string nombre,
        Guid liderParticipanteId, int puntaje, DateTime fechaCreacion,
        IEnumerable<Participante>? integrantes = null)
    {
        var equipo = new Equipo
        {
            Id = id,
            SesionId = sesionId,
            Nombre = nombre,
            LiderParticipanteId = liderParticipanteId,
            Puntaje = puntaje,
            FechaCreacion = fechaCreacion
        };
        if (integrantes is not null) equipo._participantes.AddRange(integrantes);
        return equipo;
    }
}
