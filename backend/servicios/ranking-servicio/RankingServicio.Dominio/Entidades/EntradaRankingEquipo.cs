namespace RankingServicio.Dominio.Entidades;

public sealed class EntradaRankingEquipo
{
    public Guid Id { get; private set; }
    public Guid SesionId { get; private set; }
    public Guid EquipoId { get; private set; }
    public string NombreEquipo { get; private set; } = string.Empty;
    public int PuntajeTotal { get; private set; }
    public int EtapasCompletadas { get; private set; }
    public int Posicion { get; private set; }
    public DateTime UltimaActualizacionUtc { get; private set; }

    private EntradaRankingEquipo() { }

    public static EntradaRankingEquipo Crear(
        Guid sesionId, Guid equipoId, string nombreEquipo, DateTime ahora)
        => new()
        {
            Id = Guid.NewGuid(),
            SesionId = sesionId,
            EquipoId = equipoId,
            NombreEquipo = nombreEquipo,
            PuntajeTotal = 0,
            EtapasCompletadas = 0,
            Posicion = 0,
            UltimaActualizacionUtc = ahora
        };

    public static EntradaRankingEquipo Rehidratar(
        Guid id, Guid sesionId, Guid equipoId, string nombreEquipo,
        int puntajeTotal, int etapasCompletadas, int posicion, DateTime ultimaActualizacionUtc)
        => new()
        {
            Id = id,
            SesionId = sesionId,
            EquipoId = equipoId,
            NombreEquipo = nombreEquipo,
            PuntajeTotal = puntajeTotal,
            EtapasCompletadas = etapasCompletadas,
            Posicion = posicion,
            UltimaActualizacionUtc = ultimaActualizacionUtc
        };

    public void AgregarPuntaje(int puntos, DateTime ahora)
    {
        PuntajeTotal += puntos;
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
