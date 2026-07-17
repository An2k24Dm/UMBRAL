using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos.ResultadosPuntaje;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerResultadoPuntaje;

public sealed class ObtenerResultadoPuntajeManejador
    : IRequestHandler<ObtenerResultadoPuntajeConsulta, ResultadoPuntajeDto>
{
    private const string TipoResultado = "ranking.puntaje_actualizado";

    private readonly IRepositorioRespuestasTrivia _respuestasTrivia;
    private readonly IRepositorioResultadosRankingProcesados _resultadosProcesados;

    public ObtenerResultadoPuntajeManejador(
        IRepositorioRespuestasTrivia respuestasTrivia,
        IRepositorioResultadosRankingProcesados resultadosProcesados)
    {
        _respuestasTrivia = respuestasTrivia;
        _resultadosProcesados = resultadosProcesados;
    }

    public async Task<ResultadoPuntajeDto> Handle(
        ObtenerResultadoPuntajeConsulta consulta,
        CancellationToken cancelacion)
    {
        var puntaje = await _respuestasTrivia
            .ObtenerPuntajeGanadoPorEventoAsync(consulta.EventoId, cancelacion);
        var procesado = puntaje.HasValue && await _resultadosProcesados
            .ExisteAsync(consulta.EventoId, TipoResultado, cancelacion);

        return !procesado
            ? new ResultadoPuntajeDto(consulta.EventoId, Procesado: false, PuntajeGanado: null)
            : new ResultadoPuntajeDto(
                consulta.EventoId,
                Procesado: true,
                PuntajeGanado: puntaje);
    }
}
