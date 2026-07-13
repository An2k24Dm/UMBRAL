using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Dominio.Entidades;

// Aggregate Root. Existe exactamente un Ranking por sesión (SesionId único).
// Contiene solo los participantes y equipos de ESA sesión; nunca acumula
// equipos o participantes históricos de otras sesiones. Toda modificación de
// las entidades hijas ocurre a través de este raíz, que garantiza las
// invariantes (unicidad y puntaje de equipo = suma de sus participantes).
public sealed class Ranking
{
    private readonly List<RankingParticipante> _participantes = new();
    private readonly List<RankingEquipo> _equipos = new();

    public Guid Id { get; private set; }

    public Guid SesionId { get; private set; }

    public IReadOnlyCollection<RankingParticipante> Participantes =>
        _participantes.AsReadOnly();

    public IReadOnlyCollection<RankingEquipo> Equipos =>
        _equipos.AsReadOnly();

    private Ranking() { }

    public static Ranking Crear(Guid sesionId)
    {
        if (sesionId == Guid.Empty)
            throw new RankingInvalidoExcepcion(
                "El identificador de la sesión es obligatorio.");

        return new Ranking
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId
        };
    }

    // Registra (idempotentemente) un participante en el ranking. Si ya existe
    // por su ParticipanteSesionId no se duplica; si llega su equipo, se fija.
    public RankingParticipante RegistrarParticipante(
        Guid participanteSesionId, Guid participanteIdentidadId, Guid? equipoId)
    {
        var existente = BuscarParticipante(participanteSesionId);
        if (existente is not null)
        {
            if (equipoId.HasValue && existente.EquipoId != equipoId)
                existente.EstablecerEquipo(equipoId);
            return existente;
        }

        var nuevo = RankingParticipante.Crear(
            participanteSesionId, participanteIdentidadId, equipoId);
        _participantes.Add(nuevo);
        return nuevo;
    }

    // Registra (idempotentemente) un equipo en el ranking. No se duplica por
    // EquipoId dentro del mismo Ranking.
    public RankingEquipo RegistrarEquipo(Guid equipoId)
    {
        var existente = BuscarEquipo(equipoId);
        if (existente is not null)
            return existente;

        var nuevo = RankingEquipo.Crear(equipoId);
        _equipos.Add(nuevo);
        return nuevo;
    }

    // Suma puntaje a un participante y mantiene coherente el puntaje del equipo
    // (si corresponde) recalculándolo como la suma de sus participantes.
    public void RegistrarPuntajeParticipante(
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        long puntos)
    {
        var participante = BuscarParticipante(participanteSesionId);
        if (participante is null)
        {
            participante = RankingParticipante.Crear(
                participanteSesionId, participanteIdentidadId, equipoId);
            _participantes.Add(participante);
        }
        else if (equipoId.HasValue && participante.EquipoId != equipoId)
        {
            participante.EstablecerEquipo(equipoId);
        }

        participante.AgregarPuntaje(puntos);

        if (equipoId.HasValue)
            RecalcularPuntajeEquipo(equipoId.Value);
    }

    // Participantes ordenados por puntaje descendente. Empate: orden
    // determinístico por ParticipanteSesionId para posiciones reproducibles.
    public IReadOnlyList<RankingParticipante> ParticipantesOrdenados() =>
        _participantes
            .OrderByDescending(p => p.Puntaje.Valor)
            .ThenBy(p => p.ParticipanteSesionId)
            .ToList();

    // Equipos ordenados por puntaje descendente. Empate: orden determinístico
    // por EquipoId.
    public IReadOnlyList<RankingEquipo> EquiposOrdenados() =>
        _equipos
            .OrderByDescending(e => e.Puntaje.Valor)
            .ThenBy(e => e.EquipoId)
            .ToList();

    // Participantes que aportan a un equipo (para el detalle desplegable),
    // ordenados por su aporte descendente.
    public IReadOnlyList<RankingParticipante> ParticipantesDeEquipo(Guid equipoId) =>
        _participantes
            .Where(p => p.EquipoId == equipoId)
            .OrderByDescending(p => p.Puntaje.Valor)
            .ThenBy(p => p.ParticipanteSesionId)
            .ToList();

    private void RecalcularPuntajeEquipo(Guid equipoId)
    {
        var equipo = BuscarEquipo(equipoId) ?? RegistrarEquipo(equipoId);
        var total = _participantes
            .Where(p => p.EquipoId == equipoId)
            .Aggregate(0L, (acumulado, p) => acumulado + p.Puntaje.Valor);
        equipo.EstablecerPuntaje(Puntaje.Desde(total));
    }

    private RankingParticipante? BuscarParticipante(Guid participanteSesionId) =>
        _participantes.FirstOrDefault(p => p.ParticipanteSesionId == participanteSesionId);

    private RankingEquipo? BuscarEquipo(Guid equipoId) =>
        _equipos.FirstOrDefault(e => e.EquipoId == equipoId);
}
