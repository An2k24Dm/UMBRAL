namespace RankingServicio.Dominio.Entidades;

public sealed class EntradaRankingParticipante
{
    public Guid Id { get; private set; }
    public Guid SesionId { get; private set; }
    public Guid ParticipanteIdentidadId { get; private set; }
    public string NombreParticipante { get; private set; } = string.Empty;
    public int PuntajeTotal { get; private set; }
    public int RespuestasCorrectas { get; private set; }
    public int RespuestasTotales { get; private set; }
    public int EtapasCompletadas { get; private set; }
    public int Posicion { get; private set; }
    public DateTime UltimaActualizacionUtc { get; private set; }

    private EntradaRankingParticipante() { }

    public static EntradaRankingParticipante Crear(
        Guid sesionId,
        Guid participanteIdentidadId,
        string nombreParticipante,
        DateTime ahora)
        => new()
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            NombreParticipante = nombreParticipante,
            PuntajeTotal = 0,
            RespuestasCorrectas = 0,
            RespuestasTotales = 0,
            EtapasCompletadas = 0,
            Posicion = 0,
            UltimaActualizacionUtc = ahora
        };

    public static EntradaRankingParticipante Rehidratar(
        Guid id, Guid sesionId, Guid participanteIdentidadId, string nombreParticipante,
        int puntajeTotal, int respuestasCorrectas, int respuestasTotales,
        int etapasCompletadas, int posicion, DateTime ultimaActualizacionUtc)
        => new()
        {
            Id = id,
            SesionId = sesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            NombreParticipante = nombreParticipante,
            PuntajeTotal = puntajeTotal,
            RespuestasCorrectas = respuestasCorrectas,
            RespuestasTotales = respuestasTotales,
            EtapasCompletadas = etapasCompletadas,
            Posicion = posicion,
            UltimaActualizacionUtc = ultimaActualizacionUtc
        };

    public void AgregarPuntaje(int puntos, bool esCorrecta, DateTime ahora)
    {
        PuntajeTotal += puntos;
        RespuestasTotales++;
        if (esCorrecta) RespuestasCorrectas++;
        UltimaActualizacionUtc = ahora;
    }

    public void RegistrarEtapaCompletada(DateTime ahora)
    {
        EtapasCompletadas++;
        UltimaActualizacionUtc = ahora;
    }

    public void ActualizarPosicion(int posicion)
    {
        Posicion = posicion;
    }
}
