using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Dominio.Entidades;

public sealed class Participante
{
    public Guid Id { get; private set; }
    public Guid SesionId { get; private set; }
    public Guid ParticipanteIdentidadId { get; private set; }
    public Guid? EquipoId { get; private set; }
    public PuntajeSesion Puntaje { get; private set; } = null!;
    // HU52 — Magnitud POSITIVA acumulada de penalizaciones del participante
    // (snapshot autoritativo de ranking; solo sesiones individuales). Se
    // visualiza como "-N pts".
    public int PuntosPenalizados { get; private set; }
    public DateTime? SnapshotRankingUtc { get; private set; }
    public DateTime FechaUnionSesion { get; private set; }
    public DateTime? FechaUnionEquipo { get; private set; }

    private Participante() { }

    public static Participante CrearParaSesionIndividual(
        Guid sesionId, Guid participanteIdentidadId, DateTime fechaUnionSesionUtc)
    {
        ValidarObligatorios(sesionId, participanteIdentidadId);
        return new Participante
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = null,
            Puntaje = PuntajeSesion.Cero(),
            SnapshotRankingUtc = null,
            FechaUnionSesion = fechaUnionSesionUtc,
            FechaUnionEquipo = null
        };
    }

    public static Participante CrearParaEquipo(
        Guid sesionId, Guid equipoId,
        Guid participanteIdentidadId,
        DateTime fechaUnionSesionUtc, DateTime fechaUnionEquipoUtc)
    {
        ValidarObligatorios(sesionId, participanteIdentidadId);
        if (equipoId == Guid.Empty)
            throw new ParticipacionInvalidaExcepcion(
                "El identificador del equipo es obligatorio.");

        return new Participante
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            Puntaje = PuntajeSesion.Cero(),
            SnapshotRankingUtc = null,
            FechaUnionSesion = fechaUnionSesionUtc,
            FechaUnionEquipo = fechaUnionEquipoUtc
        };
    }

    // Rehidratar recibe el entero tal como está persistido y lo reconstruye
    // con DesdePersistencia para no invalidar registros existentes.
    public static Participante Rehidratar(
        Guid id, Guid sesionId, Guid participanteIdentidadId,
        Guid? equipoId, int puntaje,
        DateTime fechaUnionSesion, DateTime? fechaUnionEquipo,
        DateTime? snapshotRankingUtc = null,
        int puntosPenalizados = 0)
        => new()
        {
            Id = id,
            SesionId = sesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            Puntaje = PuntajeSesion.DesdePersistencia(puntaje),
            PuntosPenalizados = puntosPenalizados,
            SnapshotRankingUtc = snapshotRankingUtc,
            FechaUnionSesion = fechaUnionSesion,
            FechaUnionEquipo = fechaUnionEquipo
        };

    public void SumarPuntaje(int puntos) => Puntaje = Puntaje.Sumar(puntos);

    public void SumarPuntaje(PuntajeSesion puntos) => Puntaje = Puntaje.Sumar(puntos);

    public void EstablecerPuntajeSnapshot(int puntaje)
        => Puntaje = PuntajeSesion.DesdePersistencia(puntaje);

    public bool EstablecerPuntajeSnapshot(int puntaje, DateTime calculadoEnUtc)
    {
        if (SnapshotRankingUtc.HasValue && calculadoEnUtc <= SnapshotRankingUtc.Value)
            return false;

        Puntaje = PuntajeSesion.DesdePersistencia(puntaje);
        SnapshotRankingUtc = calculadoEnUtc;
        return true;
    }

    // HU52 — Snapshot de penalización individual: fija el puntaje resultante
    // (autoritativo de ranking; puede ser negativo) y la magnitud acumulada
    // penalizada. Respeta el orden causal por SnapshotRankingUtc (el resultado
    // más reciente gana). PuntosPenalizados solo lo modifica una penalización.
    public bool EstablecerPenalizacionSnapshot(
        int puntosPenalizados, int puntajeResultante, DateTime calculadoEnUtc)
    {
        if (SnapshotRankingUtc.HasValue && calculadoEnUtc <= SnapshotRankingUtc.Value)
            return false;

        PuntosPenalizados = puntosPenalizados;
        Puntaje = PuntajeSesion.DesdePersistencia(puntajeResultante);
        SnapshotRankingUtc = calculadoEnUtc;
        return true;
    }

    private static void ValidarObligatorios(Guid sesionId, Guid participanteIdentidadId)
    {
        if (sesionId == Guid.Empty)
            throw new ParticipacionInvalidaExcepcion(
                "El identificador de la sesión es obligatorio.");
        if (participanteIdentidadId == Guid.Empty)
            throw new ParticipacionInvalidaExcepcion(
                "El identificador del participante es obligatorio.");
    }
}
