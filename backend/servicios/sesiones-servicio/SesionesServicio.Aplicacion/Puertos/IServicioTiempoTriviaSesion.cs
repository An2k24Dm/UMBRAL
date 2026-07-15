using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IServicioTiempoTriviaSesion
{
    EstadoTiempoTriviaSesion Calcular(
        EjecucionActualSesion ejecucion,
        TriviaParticipanteJuegosDto trivia,
        IReadOnlyCollection<RespuestaTriviaTiempo> respuestasPrevias,
        DateTime ahoraUtc);
}

public sealed record EstadoTiempoTriviaSesion(
    long TiempoActivoEtapaMs,
    IReadOnlyList<VentanaPreguntaTriviaSesion> Preguntas,
    Guid? PreguntaActualId,
    int? TiempoRestantePreguntaMs,
    int? TiempoTranscurridoPreguntaMs,
    bool TriviaAgotada,
    bool EnTransicionEntrePreguntas = false,
    int? TiempoRestanteTransicionMs = null,
    Guid? SiguientePreguntaId = null)
{
    public IReadOnlyList<Guid> PreguntasExpiradasIds =>
        Preguntas.Where(p => p.Expirada).Select(p => p.PreguntaId).ToList().AsReadOnly();

    public VentanaPreguntaTriviaSesion? ObtenerPregunta(Guid preguntaId)
        => Preguntas.FirstOrDefault(p => p.PreguntaId == preguntaId);
}

public sealed record VentanaPreguntaTriviaSesion(
    Guid PreguntaId,
    long InicioMs,
    long FinMs,
    int DuracionMs,
    bool Expirada,
    bool Actual);
