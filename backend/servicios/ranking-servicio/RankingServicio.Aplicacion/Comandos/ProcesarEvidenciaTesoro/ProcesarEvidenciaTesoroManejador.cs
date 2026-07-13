using MediatR;
using Microsoft.Extensions.Logging;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Abstract;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Dominio.Estrategias;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Aplicacion.Comandos.ProcesarEvidenciaTesoro;

public sealed class ProcesarEvidenciaTesoroManejador
    : IRequestHandler<ProcesarEvidenciaTesoroComando>
{
    private const string TipoEvento = "EvidenciaTesoroRegistrada";

    private readonly IRepositorioRanking _repo;
    private readonly IRepositorioEventosProcesados _repoEventos;
    private readonly IUnidadTrabajoRanking _unidadTrabajo;
    private readonly INotificadorRankingTiempoReal _notificador;
    private readonly IPublicadorResultadosPuntaje _publicadorResultados;
    private readonly IProveedorFechaHora _reloj;
    private readonly IEstrategiaCalculoPuntaje<ContextoCalculoPuntajeBusquedaTesoro> _estrategia;
    private readonly ILogger<ProcesarEvidenciaTesoroManejador> _log;

    public ProcesarEvidenciaTesoroManejador(
        IRepositorioRanking repo,
        IRepositorioEventosProcesados repoEventos,
        IUnidadTrabajoRanking unidadTrabajo,
        INotificadorRankingTiempoReal notificador,
        IPublicadorResultadosPuntaje publicadorResultados,
        IProveedorFechaHora reloj,
        IEstrategiaCalculoPuntaje<ContextoCalculoPuntajeBusquedaTesoro> estrategia,
        ILogger<ProcesarEvidenciaTesoroManejador> log)
    {
        _repo = repo;
        _repoEventos = repoEventos;
        _unidadTrabajo = unidadTrabajo;
        _notificador = notificador;
        _publicadorResultados = publicadorResultados;
        _reloj = reloj;
        _estrategia = estrategia;
        _log = log;
    }

    public async Task Handle(ProcesarEvidenciaTesoroComando comando, CancellationToken cancelacion)
    {
        if (await _repoEventos.ExisteAsync(comando.EventoId, TipoEvento, cancelacion))
            return;

        var puntajeGanado = _estrategia.Calcular(new ContextoCalculoPuntajeBusquedaTesoro(
            comando.EsValida,
            comando.PuntajeBase));

        var resultado = await RegistrarPuntajeAsync(comando, puntajeGanado, cancelacion);

        _log.LogInformation(
            "Emitiendo PuntajeCalculado Tesoro. EventoId={EventoId} SesionId={SesionId} ParticipanteSesionId={ParticipanteSesionId} ParticipanteIdentidadId={ParticipanteIdentidadId} EquipoId={EquipoId} PuntajeGanado={PuntajeGanado} PuntajeTotalParticipante={PuntajeTotalParticipante} PuntajeTotalEquipo={PuntajeTotalEquipo}",
            resultado.EventoIdOrigen,
            resultado.SesionId,
            resultado.ParticipanteSesionId,
            resultado.ParticipanteIdentidadId,
            resultado.EquipoId,
            resultado.PuntajeGanado,
            resultado.PuntajeTotalParticipante,
            resultado.PuntajeTotalEquipo);

        await _notificador.NotificarPuntajeCalculadoAsync(resultado, cancelacion);
        await _notificador.NotificarRankingParticipantesActualizadoAsync(comando.SesionId, cancelacion);
        if (comando.EquipoId.HasValue)
            await _notificador.NotificarRankingEquiposActualizadoAsync(comando.SesionId, cancelacion);
    }

    private async Task<PuntajeCalculadoDto> RegistrarPuntajeAsync(
        ProcesarEvidenciaTesoroComando comando,
        Puntaje puntajeGanado,
        CancellationToken cancelacion)
    {
        PuntajeCalculadoDto? resultado = null;
        var ahora = _reloj.ObtenerFechaHoraUtc();

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var ranking = await _repo.ObtenerPorSesionAsync(comando.SesionId, ct);
            if (ranking is null)
            {
                ranking = Ranking.Crear(comando.SesionId);
                await _repo.AgregarAsync(ranking, ct);
            }

            ranking.RegistrarPuntajeParticipante(
                comando.ParticipanteSesionId,
                comando.ParticipanteIdentidadId,
                comando.EquipoId,
                puntajeGanado);

            var participante = ranking.Participantes.Single(p =>
                p.ParticipanteSesionId == comando.ParticipanteSesionId);
            var puntajeEquipo = comando.EquipoId.HasValue
                ? ranking.Equipos.Single(e => e.EquipoId == comando.EquipoId.Value).Puntaje.Valor
                : (long?)null;

            resultado = new PuntajeCalculadoDto(
                comando.EventoId,
                comando.SesionId,
                comando.ParticipanteSesionId,
                comando.ParticipanteIdentidadId,
                comando.EquipoId,
                puntajeGanado.Valor,
                participante.Puntaje.Valor,
                puntajeEquipo,
                ahora);

            await _repo.ActualizarAsync(ranking, ct);
            await _repoEventos.RegistrarAsync(comando.EventoId, TipoEvento, ahora, ct);
            await _publicadorResultados.PublicarPuntajeActualizadoAsync(resultado, ct);
        }, cancelacion);

        return resultado!;
    }
}
