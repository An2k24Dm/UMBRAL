using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerTriviaParticipante;

// Devuelve la trivia para el participante móvil: sin EsCorrecta en opciones
// y sin datos administrativos (estado, fechaCreacion).
public sealed class ObtenerTriviaParticipanteManejador
    : IRequestHandler<ObtenerTriviaParticipanteConsulta, TriviaParticipanteDto?>
{
    private readonly IRepositorioJuegos _repositorio;

    public ObtenerTriviaParticipanteManejador(IRepositorioJuegos repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<TriviaParticipanteDto?> Handle(
        ObtenerTriviaParticipanteConsulta consulta, CancellationToken cancelacion)
    {
        var detalle = await _repositorio.ObtenerDetalleTriviaAsync(
            consulta.TriviaId, cancelacion);

        if (detalle is null) return null;

        return new TriviaParticipanteDto
        {
            Id = detalle.Id,
            Nombre = detalle.Nombre,
            Descripcion = detalle.Descripcion,
            TiempoLimitePorPregunta = detalle.TiempoLimitePorPregunta,
            Preguntas = detalle.Preguntas.Select(p => new PreguntaParticipanteDto
            {
                Id = p.Id,
                Enunciado = p.Enunciado,
                PuntajeAsignado = p.PuntajeAsignado,
                TiempoEstimado = p.TiempoEstimado,
                Opciones = p.Opciones.Select(o => new OpcionParticipanteDto
                {
                    Id = o.Id,
                    Texto = o.Texto
                }).ToList()
            }).ToList()
        };
    }
}
