using MediatR;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Comandos.ProcesarPuntaje;

public sealed class ProcesarPuntajeManejador : IRequestHandler<ProcesarPuntajeComando>
{
    private readonly IRepositorioRanking _repo;
    private readonly IRepositorioEventosProcesados _repoEventos;
    private readonly IUnidadTrabajoRanking _unidadTrabajo;
    private readonly INotificadorRankingTiempoReal _notificador;
    private readonly IProveedorFechaHora _reloj;

    public ProcesarPuntajeManejador(
        IRepositorioRanking repo,
        IRepositorioEventosProcesados repoEventos,
        IUnidadTrabajoRanking unidadTrabajo,
        INotificadorRankingTiempoReal notificador,
        IProveedorFechaHora reloj)
    {
        _repo = repo;
        _repoEventos = repoEventos;
        _unidadTrabajo = unidadTrabajo;
        _notificador = notificador;
        _reloj = reloj;
    }

    public async Task Handle(ProcesarPuntajeComando comando, CancellationToken cancelacion)
    {
        var clave = $"ProcesarPuntaje_{comando.TipoJuego}";
        if (await _repoEventos.ExisteAsync(comando.EventoId, clave, cancelacion))
            return;

        var ahora = _reloj.ObtenerFechaHoraUtc();

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var ranking = await _repo.ObtenerPorSesionAsync(comando.SesionId, ct);
            if (ranking is null)
            {
                ranking = Ranking.Crear(comando.SesionId);
                await _repo.AgregarAsync(ranking, ct);
            }

            // El agregado suma el puntaje al participante y mantiene el puntaje
            // del equipo como la suma de sus participantes.
            ranking.RegistrarPuntajeParticipante(
                comando.ParticipanteSesionId,
                comando.ParticipanteIdentidadId,
                comando.EquipoId,
                comando.Puntaje);

            await _repo.ActualizarAsync(ranking, ct);
            await _repoEventos.RegistrarAsync(comando.EventoId, clave, ahora, ct);
        }, cancelacion);

        await _notificador.NotificarRankingParticipantesActualizadoAsync(
            comando.SesionId, cancelacion);

        if (comando.EquipoId.HasValue)
            await _notificador.NotificarRankingEquiposActualizadoAsync(
                comando.SesionId, cancelacion);
    }
}
