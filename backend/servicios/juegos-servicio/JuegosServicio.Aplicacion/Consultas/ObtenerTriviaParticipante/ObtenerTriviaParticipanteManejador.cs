using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerTriviaParticipante;

public sealed class ObtenerTriviaParticipanteManejador
    : IRequestHandler<ObtenerTriviaParticipanteConsulta, TriviaParticipanteDto?>
{
    private readonly IRepositorioJuegos _repositorio;

    public ObtenerTriviaParticipanteManejador(IRepositorioJuegos repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<TriviaParticipanteDto?> Handle(
        ObtenerTriviaParticipanteConsulta consulta, CancellationToken cancelacion)
        => _repositorio.ObtenerTriviaParticipanteAsync(consulta.TriviaId, cancelacion);
}
