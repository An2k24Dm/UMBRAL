using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

internal static class MapeoParticipantePersistencia
{
    public static ParticipanteModelo HaciaModelo(Participante p) => new()
    {
        Id = p.Id,
        SesionId = p.SesionId,
        ParticipanteIdentidadId = p.ParticipanteIdentidadId,
        EquipoId = p.EquipoId,
        Puntaje = p.Puntaje.Valor,
        SnapshotRankingUtc = p.SnapshotRankingUtc,
        FechaUnionSesion = p.FechaUnionSesion,
        FechaUnionEquipo = p.FechaUnionEquipo
    };

    public static Participante HaciaDominio(ParticipanteModelo p)
        => Participante.Rehidratar(
            p.Id, p.SesionId, p.ParticipanteIdentidadId,
            p.EquipoId, p.Puntaje, p.FechaUnionSesion, p.FechaUnionEquipo,
            p.SnapshotRankingUtc);
}
