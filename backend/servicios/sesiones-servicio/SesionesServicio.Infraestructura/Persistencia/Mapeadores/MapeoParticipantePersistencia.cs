using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Infraestructura.Persistencia.Mapeadores;

// Mapeo compartido de Participante entre dominio y persistencia, reutilizado
// por las estrategias Individual y Grupal para no duplicar la conversión.
internal static class MapeoParticipantePersistencia
{
    public static ParticipanteModelo HaciaModelo(Participante p) => new()
    {
        Id = p.Id,
        SesionId = p.SesionId,
        ParticipanteIdentidadId = p.ParticipanteIdentidadId,
        EquipoId = p.EquipoId,
        Puntaje = p.Puntaje,
        FechaUnionSesion = p.FechaUnionSesion,
        FechaUnionEquipo = p.FechaUnionEquipo
    };

    public static Participante HaciaDominio(ParticipanteModelo p)
        => Participante.Rehidratar(
            p.Id, p.SesionId, p.ParticipanteIdentidadId,
            p.EquipoId, p.Puntaje, p.FechaUnionSesion, p.FechaUnionEquipo);
}
