using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Dominio.Entidades;

public sealed class RankingParticipante
{
    public Guid Id { get; private set; }
    public Guid ParticipanteSesionId { get; private set; }
    public Guid ParticipanteIdentidadId { get; private set; }
    public Guid? EquipoId { get; private set; }
    public Puntaje Puntaje { get; private set; } = Puntaje.Cero;
    public int PuntosPenalizados { get; private set; }

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
            Puntaje = Puntaje.Cero,
            PuntosPenalizados = 0
        };
    }

    internal void EstablecerEquipo(Guid? equipoId) => EquipoId = equipoId;

    internal void AgregarPuntaje(Puntaje puntaje) => Puntaje = Puntaje.Sumar(puntaje.Valor);

    internal void AplicarPenalizacion(CantidadPenalizacion penalizacion)
    {
        Puntaje = Puntaje.AplicarPenalizacion(penalizacion);
        PuntosPenalizados += penalizacion.Valor;
    }
}
