using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Dominio.Entidades;

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

    public RankingEquipo RegistrarEquipo(Guid equipoId)
    {
        var existente = BuscarEquipo(equipoId);
        if (existente is not null)
            return existente;

        var nuevo = RankingEquipo.Crear(equipoId);
        _equipos.Add(nuevo);
        return nuevo;
    }

    public void RegistrarPuntajeParticipante(
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        long puntos)
        => RegistrarPuntajeParticipante(
            participanteSesionId,
            participanteIdentidadId,
            equipoId,
            Puntaje.Desde(puntos));

    public void RegistrarPuntajeParticipante(
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        Puntaje puntaje)
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

        participante.AgregarPuntaje(puntaje);

        if (equipoId.HasValue)
            RecalcularPuntajeEquipo(equipoId.Value);
    }

    public IReadOnlyList<RankingParticipante> ParticipantesOrdenados() =>
        _participantes
            .OrderByDescending(p => p.Puntaje.Valor)
            .ThenBy(p => p.ParticipanteSesionId)
            .ToList();

    public IReadOnlyList<RankingEquipo> EquiposOrdenados() =>
        _equipos
            .OrderByDescending(e => e.Puntaje.Valor)
            .ThenBy(e => e.EquipoId)
            .ToList();

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
