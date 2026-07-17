using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Commons.Dtos.DesgloseSesion;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Consultas.ObtenerMiDesgloseSesion;

public sealed class ObtenerMiDesgloseSesionManejador
    : IRequestHandler<ObtenerMiDesgloseSesionConsulta, MiDesgloseSesionDto>
{
    private readonly IRepositorioSesiones _repositorio;
    private readonly IRepositorioPenalizacionesAplicadas _penalizaciones;
    private readonly IRepositorioRespuestasTrivia _respuestasTrivia;
    private readonly IRepositorioEvidenciasTesoro _evidenciasTesoro;
    private readonly IClienteJuegosMisiones _clienteMisiones;
    private readonly IUsuarioActual _usuarioActual;

    public ObtenerMiDesgloseSesionManejador(
        IRepositorioSesiones repositorio,
        IRepositorioPenalizacionesAplicadas penalizaciones,
        IRepositorioRespuestasTrivia respuestasTrivia,
        IRepositorioEvidenciasTesoro evidenciasTesoro,
        IClienteJuegosMisiones clienteMisiones,
        IUsuarioActual usuarioActual)
    {
        _repositorio = repositorio;
        _penalizaciones = penalizaciones;
        _respuestasTrivia = respuestasTrivia;
        _evidenciasTesoro = evidenciasTesoro;
        _clienteMisiones = clienteMisiones;
        _usuarioActual = usuarioActual;
    }

    public async Task<MiDesgloseSesionDto> Handle(
        ObtenerMiDesgloseSesionConsulta consulta, CancellationToken cancelacion)
    {
        var identidadId = _usuarioActual.ObtenerId() ?? Guid.Empty;
        var vacio = new MiDesgloseSesionDto(identidadId, 0, 0, 0, new List<DesgloseMisionDto>());
        if (identidadId == Guid.Empty)
            return vacio;

        var sesion = await _repositorio.ObtenerPorIdAsync(consulta.SesionId, cancelacion);
        if (sesion is null)
            return vacio;

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

        var puntajeBruto = puntajePorEtapa.Values.Sum();

        var (puntosPenalizados, puntajeTotal) = await CalcularPenalizacionYTotalAsync(
            sesion, identidadId, puntajeBruto, cancelacion);

        return new MiDesgloseSesionDto(
            identidadId, puntajeBruto, puntosPenalizados, puntajeTotal, misiones);
    }

    private async Task<(long PuntosPenalizados, long PuntajeTotal)> CalcularPenalizacionYTotalAsync(
        Sesion sesionBase,
        Guid identidadId,
        long puntajeBruto,
        CancellationToken cancelacion)
    {
        if (sesionBase is SesionIndividual individual)
        {
            var participante = individual.Participantes
                .FirstOrDefault(p => p.ParticipanteIdentidadId == identidadId);
            long penalizados = participante is null
                ? 0
                : await _penalizaciones.SumarPuntosPorParticipanteAsync(
                    sesionBase.Id, identidadId, cancelacion);
            return (penalizados, puntajeBruto - penalizados);
        }

        if (sesionBase is SesionGrupal grupal)
        {
            var equipo = grupal.Equipos
                .FirstOrDefault(e => e.ContieneParticipanteIdentidadId(identidadId));
            if (equipo is null)
                return (0, puntajeBruto);

            long penalizados = await _penalizaciones.SumarPuntosPorEquipoAsync(
                sesionBase.Id, equipo.Id, cancelacion);
            var brutoEquipoTrivia = await _respuestasTrivia.ObtenerPuntajeGanadoEquipoAsync(
                sesionBase.Id, equipo.Id, cancelacion);
            var brutoEquipoTesoro = await _evidenciasTesoro.ObtenerPuntajeGanadoEquipoAsync(
                sesionBase.Id, equipo.Id, cancelacion);
            var brutoEquipo = brutoEquipoTrivia + brutoEquipoTesoro;
            return (penalizados, brutoEquipo - penalizados);
        }

        return (0, puntajeBruto);
    }
}
