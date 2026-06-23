using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerDetalleMision;

public sealed class ObtenerDetalleMisionManejador
    : IRequestHandler<ObtenerDetalleMisionConsulta, MisionDetalleDto?>
{
    private readonly IRepositorioMisiones _repositorio;

    public ObtenerDetalleMisionManejador(IRepositorioMisiones repositorio)
    {
        _repositorio = repositorio;
    }

    public Task<MisionDetalleDto?> Handle(
        ObtenerDetalleMisionConsulta consulta, CancellationToken cancelacion) =>
        _repositorio.ObtenerDetalleMisionAsync(consulta.MisionId, cancelacion);
}
