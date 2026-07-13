using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Dominio.Entidades;

// Entidad hija del agregado Ranking. Representa la participación de una persona
// dentro del ranking de UNA sesión. No conoce la sesión (pertenece al Ranking
// padre) ni almacena nombres/alias ni estadísticas que son propiedad de otros
// bounded contexts. Solo el agregado Ranking puede crearla o mutarla (métodos
// internos), garantizando las invariantes.
public sealed class RankingParticipante
{
    public Guid Id { get; private set; }

    // Identifica la participación concreta dentro de la sesión
    // (Participante.Id en sesiones-servicio). Clave única dentro del Ranking.
    public Guid ParticipanteSesionId { get; private set; }

    // Identifica al usuario de identidad-servicio; permite agrupar sus
    // resultados en distintas sesiones para el ranking global.
    public Guid ParticipanteIdentidadId { get; private set; }

    // Null en sesiones individuales; con valor cuando el participante forma
    // parte de un equipo en una sesión grupal.
    public Guid? EquipoId { get; private set; }

    public Puntaje Puntaje { get; private set; } = Puntaje.Cero;

    private RankingParticipante() { }

    internal static RankingParticipante Crear(
        Guid participanteSesionId, Guid participanteIdentidadId, Guid? equipoId)
    {
        if (participanteSesionId == Guid.Empty)
            throw new RankingInvalidoExcepcion(
                "El identificador de participación en la sesión es obligatorio.");
        if (participanteIdentidadId == Guid.Empty)
            throw new RankingInvalidoExcepcion(
                "El identificador de identidad del participante es obligatorio.");

        return new RankingParticipante
        {
            Id = Guid.NewGuid(),
            ParticipanteSesionId = participanteSesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            Puntaje = Puntaje.Cero
        };
    }

    internal void EstablecerEquipo(Guid? equipoId) => EquipoId = equipoId;

    internal void AgregarPuntaje(long puntos) => Puntaje = Puntaje.Sumar(puntos);
}
