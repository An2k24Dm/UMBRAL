using MediatR;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Comandos.ProcesarSesionFinalizada;

public sealed class ProcesarSesionFinalizadaManejador
    : IRequestHandler<ProcesarSesionFinalizadaComando>
{
    private const string TipoEvento = "SesionFinalizada";

    private readonly IRepositorioRankingParticipante _repoParticipante;
    private readonly IRepositorioRankingEquipo _repoEquipo;
    private readonly IRepositorioRankingGlobal _repoGlobal;
    private readonly IRepositorioEventosProcesados _repoEventos;
    private readonly IUnidadTrabajoRanking _unidadTrabajo;
    private readonly IProveedorFechaHora _reloj;

    public ProcesarSesionFinalizadaManejador(
        IRepositorioRankingParticipante repoParticipante,
        IRepositorioRankingEquipo repoEquipo,
        IRepositorioRankingGlobal repoGlobal,
        IRepositorioEventosProcesados repoEventos,
        IUnidadTrabajoRanking unidadTrabajo,
        IProveedorFechaHora reloj)
    {
        _repoParticipante = repoParticipante;
        _repoEquipo = repoEquipo;
        _repoGlobal = repoGlobal;
        _repoEventos = repoEventos;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
    }

    public async Task Handle(
        ProcesarSesionFinalizadaComando comando, CancellationToken cancelacion)
    {
        if (await _repoEventos.ExisteAsync(comando.EventoId, TipoEvento, cancelacion))
            return;

        var ahora = _reloj.ObtenerFechaHoraUtc();

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var participantes = await _repoParticipante.ObtenerPorSesionAsync(
                comando.SesionId, ct);

            // Para sesiones grupales los puntos del equipo ya están en
            // la entrada individual (se acumularon en ProcesarPuntaje).
            // Cada participante aporta al ranking global lo que acumuló en la sesión.
            foreach (var p in participantes)
            {
                var global = await _repoGlobal.ObtenerPorParticipanteAsync(
                    p.ParticipanteIdentidadId, ct);

                if (global is null)
                {
                    global = RankingGlobalParticipante.Crear(
                        p.ParticipanteIdentidadId, p.NombreParticipante, ahora);
                    global.AgregarPuntajeSesion(p.PuntajeTotal, p.EtapasCompletadas, ahora);
                    await _repoGlobal.AgregarAsync(global, ct);
                }
                else
                {
                    global.AgregarPuntajeSesion(p.PuntajeTotal, p.EtapasCompletadas, ahora);
                    await _repoGlobal.ActualizarAsync(global, ct);
                }
            }

            await _repoEventos.RegistrarAsync(comando.EventoId, TipoEvento, ahora, ct);
        }, cancelacion);
    }
}
