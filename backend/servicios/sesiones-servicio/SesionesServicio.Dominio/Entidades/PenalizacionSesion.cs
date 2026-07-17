using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Dominio.Entidades;

public sealed class PenalizacionSesion
{
    public const int MotivoMaximoCaracteres = 500;

    public Guid Id { get; private set; }
    public Guid EventoId { get; private set; }
    public Guid SesionId { get; private set; }
    public TipoObjetivoPenalizacion TipoObjetivo { get; private set; }
    public Guid? ParticipanteSesionId { get; private set; }
    public Guid? ParticipanteIdentidadId { get; private set; }
    public Guid? EquipoId { get; private set; }
    public int Puntos { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
    public Guid OperadorIdentidadId { get; private set; }
    public DateTime AplicadaEnUtc { get; private set; }
    public DateTime? ProcesadaEnUtc { get; private set; }
    public long? PuntajeResultante { get; private set; }
    public EstadoProcesamientoPenalizacion EstadoProcesamiento { get; private set; }

    private PenalizacionSesion() { }

    public static PenalizacionSesion CrearParaParticipante(
        Guid eventoId,
        Guid sesionId,
        Guid participanteSesionId,
        Guid participanteIdentidadId,
        int puntos,
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
            puntos, motivo, operadorIdentidadId, aplicadaEnUtc);
    }

    public static PenalizacionSesion CrearParaEquipo(
        Guid eventoId,
        Guid sesionId,
        Guid equipoId,
        int puntos,
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
            puntos, motivo, operadorIdentidadId, aplicadaEnUtc);
    }

    private static PenalizacionSesion Crear(
        Guid eventoId,
        Guid sesionId,
        TipoObjetivoPenalizacion tipoObjetivo,
        Guid? participanteSesionId,
        Guid? participanteIdentidadId,
        Guid? equipoId,
        int puntos,
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

        // La cantidad valida 1..100 (entero). Se persiste la magnitud positiva.
        var cantidad = CantidadPenalizacion.Crear(puntos);
        var motivoNormalizado = NormalizarMotivo(motivo);

        return new PenalizacionSesion
        {
            Id = Guid.NewGuid(),
            EventoId = eventoId,
            SesionId = sesionId,
            TipoObjetivo = tipoObjetivo,
            ParticipanteSesionId = participanteSesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            Puntos = cantidad.Valor,
            Motivo = motivoNormalizado,
            OperadorIdentidadId = operadorIdentidadId,
            AplicadaEnUtc = aplicadaEnUtc,
            ProcesadaEnUtc = null,
            PuntajeResultante = null,
            EstadoProcesamiento = EstadoProcesamientoPenalizacion.Pendiente
        };
    }

    // HU52 — El motivo es obligatorio, no puede ser null/ vacío/ solo espacios,
    // se normaliza con Trim y no puede superar 500 caracteres (se rechaza, no se
    // trunca silenciosamente). La longitud se mide sobre el texto ya normalizado.
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

    // HU52 — Idempotente: solo marca Procesada la primera vez. Devuelve true si
    // aplicó el cambio; false si ya estaba procesada (resultado duplicado).
    public bool MarcarProcesada(long puntajeResultante, DateTime procesadaEnUtc)
    {
        if (EstadoProcesamiento == EstadoProcesamientoPenalizacion.Procesada)
            return false;

        EstadoProcesamiento = EstadoProcesamientoPenalizacion.Procesada;
        PuntajeResultante = puntajeResultante;
        ProcesadaEnUtc = procesadaEnUtc;
        return true;
    }

    public static PenalizacionSesion Rehidratar(
        Guid id,
        Guid eventoId,
        Guid sesionId,
        TipoObjetivoPenalizacion tipoObjetivo,
        Guid? participanteSesionId,
        Guid? participanteIdentidadId,
        Guid? equipoId,
        int puntos,
        string motivo,
        Guid operadorIdentidadId,
        DateTime aplicadaEnUtc,
        DateTime? procesadaEnUtc,
        long? puntajeResultante,
        EstadoProcesamientoPenalizacion estadoProcesamiento)
        => new()
        {
            Id = id,
            EventoId = eventoId,
            SesionId = sesionId,
            TipoObjetivo = tipoObjetivo,
            ParticipanteSesionId = participanteSesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            Puntos = puntos,
            Motivo = motivo,
            OperadorIdentidadId = operadorIdentidadId,
            AplicadaEnUtc = aplicadaEnUtc,
            ProcesadaEnUtc = procesadaEnUtc,
            PuntajeResultante = puntajeResultante,
            EstadoProcesamiento = estadoProcesamiento
        };
}
