using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Dominio.Entidades;

public sealed class Participante
{
    public Guid Id { get; private set; }
    public Guid SesionId { get; private set; }
    public Guid ParticipanteIdentidadId { get; private set; }
    public Guid? EquipoId { get; private set; }
    public int Puntaje { get; private set; }
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
            Puntaje = 0,
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
            Puntaje = 0,
            FechaUnionSesion = fechaUnionSesionUtc,
            FechaUnionEquipo = fechaUnionEquipoUtc
        };
    }

    public static Participante Rehidratar(
        Guid id, Guid sesionId, Guid participanteIdentidadId,
        Guid? equipoId, int puntaje,
        DateTime fechaUnionSesion, DateTime? fechaUnionEquipo)
        => new()
        {
            Id = id,
            SesionId = sesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            Puntaje = puntaje,
            FechaUnionSesion = fechaUnionSesion,
            FechaUnionEquipo = fechaUnionEquipo
        };

    public void SumarPuntaje(int puntos)
    {
        if (puntos < 0)
            throw new ParticipacionInvalidaExcepcion(
                "El puntaje a sumar no puede ser negativo.");
        Puntaje += puntos;
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
