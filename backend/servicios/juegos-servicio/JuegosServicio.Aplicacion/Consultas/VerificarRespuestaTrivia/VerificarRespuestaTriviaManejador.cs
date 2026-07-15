using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.VerificarRespuestaTrivia;

public sealed class VerificarRespuestaTriviaManejador
    : IRequestHandler<VerificarRespuestaTriviaConsulta, VerificacionRespuestaTriviaDto?>
{
    private readonly IRepositorioJuegos _repositorio;

    public VerificarRespuestaTriviaManejador(IRepositorioJuegos repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<VerificacionRespuestaTriviaDto?> Handle(
        VerificarRespuestaTriviaConsulta consulta, CancellationToken cancelacion)
        => _repositorio.VerificarRespuestaAsync(
            consulta.TriviaId, consulta.PreguntaId, consulta.OpcionSeleccionadaId, cancelacion);
}
