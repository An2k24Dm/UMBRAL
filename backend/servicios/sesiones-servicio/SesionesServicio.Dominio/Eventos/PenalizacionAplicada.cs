using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Dominio.Eventos;

public sealed class PenalizacionAplicada
{
    public const int MotivoMaximoCaracteres = 500;

    public Guid EventoId { get; private set; }
    public Guid SesionId { get; private set; }
    public TipoObjetivoPenalizacion TipoObjetivo { get; private set; }
    public Guid? ParticipanteSesionId { get; private set; }
    public Guid? ParticipanteIdentidadId { get; private set; }
    public Guid? EquipoId { get; private set; }
    public int PuntosDescontados { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public Guid OperadorIdentidadId { get; private set; }
    public DateTime AplicadaEnUtc { get; private set; }

    private PenalizacionAplicada() { }

    public static PenalizacionAplicada CrearParaParticipante(
        Guid eventoId,
        Guid sesionId,
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        int puntosDescontados,
        string motivo,
        Guid operadorIdentidadId,
        DateTime aplicadaEnUtc)
    {
        if (participanteSesionId == Guid.Empty)
            throw new PenalizacionInvalidaExcepcion(
                "El identificador del participante es obligatorio.");
        if (participanteIdentidadId == Guid.Empty)
            throw new PenalizacionInvalidaExcepcion(
                "El identificador de identidad del participante es obligatorio.");

        return Crear(
            eventoId, sesionId, TipoObjetivoPenalizacion.Participante,
            participanteSesionId, participanteIdentidadId, equipoId: null,
            puntosDescontados, motivo, operadorIdentidadId, aplicadaEnUtc);
    }

    public static PenalizacionAplicada CrearParaEquipo(
        Guid eventoId,
        Guid sesionId,
        Guid equipoId,
        int puntosDescontados,
        string motivo,
        Guid operadorIdentidadId,
        DateTime aplicadaEnUtc)
    {
        if (equipoId == Guid.Empty)
            throw new PenalizacionInvalidaExcepcion(
                "El identificador del equipo es obligatorio.");

        return Crear(
            eventoId, sesionId, TipoObjetivoPenalizacion.Equipo,
            participanteSesionId: null, participanteIdentidadId: null, equipoId,
            puntosDescontados, motivo, operadorIdentidadId, aplicadaEnUtc);
    }

    private static PenalizacionAplicada Crear(
        Guid eventoId,
        Guid sesionId,
        TipoObjetivoPenalizacion tipoObjetivo,
        Guid? participanteSesionId,
        Guid? participanteIdentidadId,
        Guid? equipoId,
        int puntosDescontados,
        string motivo,
        Guid operadorIdentidadId,
        DateTime aplicadaEnUtc)
    {
        if (eventoId == Guid.Empty)
            throw new PenalizacionInvalidaExcepcion("El identificador del evento es obligatorio.");
        if (sesionId == Guid.Empty)
            throw new PenalizacionInvalidaExcepcion("El identificador de la sesión es obligatorio.");
        if (operadorIdentidadId == Guid.Empty)
            throw new PenalizacionInvalidaExcepcion("El identificador del Operador es obligatorio.");

        var cantidad = CantidadPenalizacion.Crear(puntosDescontados);
        var motivoNormalizado = NormalizarMotivo(motivo);

        return new PenalizacionAplicada
        {
            EventoId = eventoId,
            SesionId = sesionId,
            TipoObjetivo = tipoObjetivo,
            ParticipanteSesionId = participanteSesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            PuntosDescontados = cantidad.Valor,
            Motivo = motivoNormalizado,
            OperadorIdentidadId = operadorIdentidadId,
            AplicadaEnUtc = aplicadaEnUtc
        };
    }

    public static string NormalizarMotivo(string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new PenalizacionInvalidaExcepcion("El motivo de la penalización es obligatorio.");

        var normalizado = motivo.Trim();
        if (normalizado.Length > MotivoMaximoCaracteres)
            throw new PenalizacionInvalidaExcepcion(
                $"El motivo no puede superar {MotivoMaximoCaracteres} caracteres.");

        return normalizado;
    }

    public static PenalizacionAplicada Rehidratar(
        Guid eventoId,
        Guid sesionId,
        TipoObjetivoPenalizacion tipoObjetivo,
        Guid? participanteSesionId,
        Guid? participanteIdentidadId,
        Guid? equipoId,
        int puntosDescontados,
        string motivo,
        Guid operadorIdentidadId,
        DateTime aplicadaEnUtc)
        => new()
        {
            EventoId = eventoId,
            SesionId = sesionId,
            TipoObjetivo = tipoObjetivo,
            ParticipanteSesionId = participanteSesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            PuntosDescontados = puntosDescontados,
            Motivo = motivo,
            OperadorIdentidadId = operadorIdentidadId,
            AplicadaEnUtc = aplicadaEnUtc
        };
}
