using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerBusquedaTesoroConPistas;

public sealed class ObtenerBusquedaTesoroConPistasManejador
    : IRequestHandler<ObtenerBusquedaTesoroConPistasConsulta, BusquedaTesoroConPistasDto?>
{
    private readonly IClienteBusquedaTesoro _clienteTesoro;
    private readonly IRepositorioPistasLiberadas _repositorioPistas;
    private readonly IRepositorioEvidenciasTesoro _repositorioEvidencias;

    public ObtenerBusquedaTesoroConPistasManejador(
        IClienteBusquedaTesoro clienteTesoro,
        IRepositorioPistasLiberadas repositorioPistas,
        IRepositorioEvidenciasTesoro repositorioEvidencias)
    {
        _clienteTesoro = clienteTesoro;
        _repositorioPistas = repositorioPistas;
        _repositorioEvidencias = repositorioEvidencias;
    }

    public async Task<BusquedaTesoroConPistasDto?> Handle(
        ObtenerBusquedaTesoroConPistasConsulta consulta, CancellationToken cancelacion)
    {
        var busqueda = await _clienteTesoro.ObtenerBusquedaParticipanteAsync(
            consulta.BusquedaId, cancelacion);
        if (busqueda is null) return null;

        var pistasLiberadas = await _repositorioPistas.ObtenerPorEtapaAsync(
            consulta.SesionId, consulta.EtapaId, cancelacion);

        var yaEnvio = await _repositorioEvidencias.ExisteEvidenciaAsync(
            consulta.SesionId, consulta.EtapaId, consulta.ParticipanteIdentidadId, cancelacion);

        return new BusquedaTesoroConPistasDto
        {
            Id = busqueda.Id,
            Nombre = busqueda.Nombre,
            Descripcion = busqueda.Descripcion,
            TiempoSegundos = busqueda.Tiempo,
            PuntajeBase = busqueda.Puntaje,
            PistasLiberadas = pistasLiberadas.Select(p => new PistaLiberadaSesionDto
            {
                PistaId = p.PistaId,
                Contenido = p.Contenido,
                FechaLiberacionUtc = p.FechaLiberacionUtc
            }).ToList(),
            YaEnvioEvidencia = yaEnvio
        };
    }
}
