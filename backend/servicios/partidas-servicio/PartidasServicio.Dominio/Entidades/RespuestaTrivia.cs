using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.Dominio.Entidades;

public sealed class RespuestaTrivia
{
    public Guid Id { get; private set; }
    public Guid SesionId { get; private set; }
    public Guid MisionId { get; private set; }
    public Guid EtapaId { get; private set; }
    public Guid PreguntaId { get; private set; }
    public Guid OpcionSeleccionadaId { get; private set; }
    public Guid ParticipanteId { get; private set; }
    public Guid? EquipoId { get; private set; }
    public bool EsCorrecta { get; private set; }
    public int PuntosGanados { get; private set; }
    public long TiempoTardadoMs { get; private set; }
    public DateTime FechaRespuestaUtc { get; private set; }

    private RespuestaTrivia() { }

    public static RespuestaTrivia Crear(
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        Guid preguntaId,
        Guid opcionSeleccionadaId,
        Guid participanteId,
        Guid? equipoId,
        bool esCorrecta,
        int puntosGanados,
        long tiempoTardadoMs,
        DateTime fechaRespuestaUtc)
    {
        if (sesionId == Guid.Empty)
            throw new ExcepcionDominio("El identificador de la sesión es obligatorio.");
        if (preguntaId == Guid.Empty)
            throw new ExcepcionDominio("El identificador de la pregunta es obligatorio.");
        if (participanteId == Guid.Empty)
            throw new ExcepcionDominio("El identificador del participante es obligatorio.");
        if (tiempoTardadoMs < 0)
            throw new ExcepcionDominio("El tiempo tardado no puede ser negativo.");

        return new RespuestaTrivia
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            MisionId = misionId,
            EtapaId = etapaId,
            PreguntaId = preguntaId,
            OpcionSeleccionadaId = opcionSeleccionadaId,
            ParticipanteId = participanteId,
            EquipoId = equipoId,
            EsCorrecta = esCorrecta,
            PuntosGanados = puntosGanados,
            TiempoTardadoMs = tiempoTardadoMs,
            FechaRespuestaUtc = fechaRespuestaUtc
        };
    }

    public static RespuestaTrivia Reconstituir(
        Guid id,
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        Guid preguntaId,
        Guid opcionSeleccionadaId,
        Guid participanteId,
        Guid? equipoId,
        bool esCorrecta,
        int puntosGanados,
        long tiempoTardadoMs,
        DateTime fechaRespuestaUtc)
        => new()
        {
            Id = id,
            SesionId = sesionId,
            MisionId = misionId,
            EtapaId = etapaId,
            PreguntaId = preguntaId,
            OpcionSeleccionadaId = opcionSeleccionadaId,
            ParticipanteId = participanteId,
            EquipoId = equipoId,
            EsCorrecta = esCorrecta,
            PuntosGanados = puntosGanados,
            TiempoTardadoMs = tiempoTardadoMs,
            FechaRespuestaUtc = fechaRespuestaUtc
        };
}
