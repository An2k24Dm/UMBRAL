using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using MediatR;

namespace JuegosServicio.Aplicacion.Consultas.ObtenerBusquedaParticipante;

public sealed class ObtenerBusquedaParticipanteManejador
    : IRequestHandler<ObtenerBusquedaParticipanteConsulta, BusquedaTesoroParticipanteDto?>
{
    private readonly IRepositorioBusquedas _repositorio;

    public ObtenerBusquedaParticipanteManejador(IRepositorioBusquedas repositorio) =>
        _repositorio = repositorio;

    public Task<BusquedaTesoroParticipanteDto?> Handle(
        ObtenerBusquedaParticipanteConsulta consulta, CancellationToken cancelacion)
        => _repositorio.ObtenerBusquedaParaParticipanteAsync(consulta.BusquedaId, cancelacion);
}
