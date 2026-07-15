using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerMiDesgloseSesion;

public sealed class ObtenerMiDesgloseSesionManejador
    : IRequestHandler<ObtenerMiDesgloseSesionConsulta, MiDesgloseSesionDto>
{
    private readonly IRepositorioSesiones _repositorio;
    private readonly IRepositorioRespuestasTrivia _respuestasTrivia;
    private readonly IRepositorioEvidenciasTesoro _evidenciasTesoro;
    private readonly IClienteJuegosMisiones _clienteMisiones;
    private readonly IUsuarioActual _usuarioActual;

    public ObtenerMiDesgloseSesionManejador(
        IRepositorioSesiones repositorio,
        IRepositorioRespuestasTrivia respuestasTrivia,
        IRepositorioEvidenciasTesoro evidenciasTesoro,
        IClienteJuegosMisiones clienteMisiones,
        IUsuarioActual usuarioActual)
    {
        _repositorio = repositorio;
        _respuestasTrivia = respuestasTrivia;
        _evidenciasTesoro = evidenciasTesoro;
        _clienteMisiones = clienteMisiones;
        _usuarioActual = usuarioActual;
    }

    public async Task<MiDesgloseSesionDto> Handle(
        ObtenerMiDesgloseSesionConsulta consulta, CancellationToken cancelacion)
    {
        var identidadId = _usuarioActual.ObtenerId() ?? Guid.Empty;
        var vacio = new MiDesgloseSesionDto(identidadId, 0, new List<DesgloseMisionDto>());
        if (identidadId == Guid.Empty)
            return vacio;

        var sesion = await _repositorio.ObtenerPorIdAsync(consulta.SesionId, cancelacion);
        if (sesion is null)
            return vacio;

        // Puntaje real por etapa del participante (Trivia + Tesoro). SUM ya viene
        // agrupado por misión/etapa; se combina por etapa.
        var puntajesTrivia = await _respuestasTrivia.ObtenerPuntajePorEtapaParticipanteAsync(
            consulta.SesionId, identidadId, cancelacion);
        var puntajesTesoro = await _evidenciasTesoro.ObtenerPuntajePorEtapaParticipanteAsync(
            consulta.SesionId, identidadId, cancelacion);

        var puntajePorEtapa = new Dictionary<Guid, long>();
        foreach (var item in puntajesTrivia.Concat(puntajesTesoro))
        {
            puntajePorEtapa.TryGetValue(item.EtapaId, out var acumulado);
            puntajePorEtapa[item.EtapaId] = acumulado + item.Puntaje;
        }

        // Estructura ordenada de misiones/etapas (nombres/orden desde juegos).
        var misionesEnOrden = sesion.Misiones.OrderBy(m => m.Orden).ToList();
        var remotas = await Task.WhenAll(misionesEnOrden
            .Select(m => _clienteMisiones.ObtenerMisionConEtapasAsync(m.MisionId, cancelacion)));

        var misiones = new List<DesgloseMisionDto>(misionesEnOrden.Count);
        for (var i = 0; i < misionesEnOrden.Count; i++)
        {
            var asociacion = misionesEnOrden[i];
            var remota = remotas[i];

            var etapas = (remota?.Etapas ?? new List<EtapaJuegosDto>())
                .OrderBy(e => e.Orden)
                .Select(e => new DesgloseEtapaDto(
                    e.Id,
                    e.Orden,
                    e.NombreModoDeJuego,
                    e.TipoModoDeJuego,
                    puntajePorEtapa.TryGetValue(e.Id, out var p) ? p : 0))
                .ToList();

            misiones.Add(new DesgloseMisionDto(
                asociacion.MisionId,
                asociacion.Orden,
                remota?.Nombre ?? string.Empty,
                etapas.Sum(x => x.Puntaje),
                etapas));
        }

        // Total autoritativo del participante (suma real de sus puntajes),
        // independiente de que la estructura de juegos esté completa.
        var total = puntajePorEtapa.Values.Sum();

        return new MiDesgloseSesionDto(identidadId, total, misiones);
    }
}
