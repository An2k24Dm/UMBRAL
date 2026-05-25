using JuegosServicio.Aplicacion.CasosDeUso.Consultas;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerTriviasEnBorradorManejador
    : IRequestHandler<ObtenerTriviasEnBorradorConsulta, List<TriviaResumenDto>>
{
    private readonly IRepositorioJuegos _repositorio;

    public ObtenerTriviasEnBorradorManejador(IRepositorioJuegos repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<List<TriviaResumenDto>> Handle(
        ObtenerTriviasEnBorradorConsulta consulta, CancellationToken cancelacion)
    {
        return _repositorio.ObtenerTriviasEnBorradorAsync(consulta.OperadorId, cancelacion);
    }
}
