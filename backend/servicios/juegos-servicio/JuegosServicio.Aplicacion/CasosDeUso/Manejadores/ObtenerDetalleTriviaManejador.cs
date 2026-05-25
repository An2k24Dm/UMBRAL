using JuegosServicio.Aplicacion.CasosDeUso.Consultas;
using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ObtenerDetalleTriviaManejador
    : IRequestHandler<ObtenerDetalleTriviaConsulta, TriviaDetalleDto?>
{
    private readonly IRepositorioJuegos _repositorio;

    public ObtenerDetalleTriviaManejador(IRepositorioJuegos repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<TriviaDetalleDto?> Handle(
        ObtenerDetalleTriviaConsulta consulta, CancellationToken cancelacion)
    {
        return _repositorio.ObtenerDetalleTriviaAsync(consulta.TriviaId, cancelacion);
    }
}
