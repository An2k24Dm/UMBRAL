namespace RankingServicio.Dominio.Entidades;

public sealed class RankingGlobalParticipante
{
    public Guid Id { get; private set; }
    public Guid ParticipanteIdentidadId { get; private set; }
    public string NombreParticipante { get; private set; } = string.Empty;
    public long PuntajeAcumulado { get; private set; }
    public int SesionesJugadas { get; private set; }
    public int EtapasCompletadasTotal { get; private set; }
    public DateTime UltimaActualizacionUtc { get; private set; }

    private RankingGlobalParticipante() { }

    public static RankingGlobalParticipante Crear(
        Guid participanteIdentidadId, string nombreParticipante, DateTime ahora)
        => new()
        {
            Id = Guid.NewGuid(),
            ParticipanteIdentidadId = participanteIdentidadId,
            NombreParticipante = nombreParticipante,
            PuntajeAcumulado = 0,
            SesionesJugadas = 0,
            EtapasCompletadasTotal = 0,
            UltimaActualizacionUtc = ahora
        };

    public static RankingGlobalParticipante Rehidratar(
        Guid id, Guid participanteIdentidadId, string nombreParticipante,
        long puntajeAcumulado, int sesionesJugadas, int etapasCompletadasTotal,
        DateTime ultimaActualizacionUtc)
        => new()
        {
            Id = id,
            ParticipanteIdentidadId = participanteIdentidadId,
            NombreParticipante = nombreParticipante,
            PuntajeAcumulado = puntajeAcumulado,
            SesionesJugadas = sesionesJugadas,
            EtapasCompletadasTotal = etapasCompletadasTotal,
            UltimaActualizacionUtc = ultimaActualizacionUtc
        };

    public void AgregarPuntajeSesion(long puntaje, int etapasCompletadas, DateTime ahora)
    {
        PuntajeAcumulado += puntaje;
        SesionesJugadas++;
        EtapasCompletadasTotal += etapasCompletadas;
        UltimaActualizacionUtc = ahora;
    }

    public void ActualizarNombre(string nombre)
    {
        NombreParticipante = nombre;
    }
}
