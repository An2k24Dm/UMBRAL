using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerTriviasActivas;

public sealed class ObtenerTriviasActivasManejador : IRequestHandler<ObtenerTriviasActivasConsulta, List<TriviaResumenDto>>
{
    private readonly IRepositorioJuegos _repositorio;

    public ObtenerTriviasActivasManejador(IRepositorioJuegos repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<List<TriviaResumenDto>> Handle(
        ObtenerTriviasActivasConsulta consulta, CancellationToken cancelacion)
    {
        return await _repositorio.ObtenerTriviasActivasAsync(cancelacion);
    }
}
