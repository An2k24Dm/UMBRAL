using MediatR;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Comandos.ProcesarParticipanteUnido;

public sealed class ProcesarParticipanteUnidoManejador
    : IRequestHandler<ProcesarParticipanteUnidoComando>
{
    private const string TipoEvento = "ParticipanteUnidoSesion";

    private readonly IRepositorioRanking _repo;
    private readonly IRepositorioEventosProcesados _repoEventos;
    private readonly IUnidadTrabajoRanking _unidadTrabajo;
    private readonly IProveedorFechaHora _reloj;

    public ProcesarParticipanteUnidoManejador(
        IRepositorioRanking repo,
        IRepositorioEventosProcesados repoEventos,
        IUnidadTrabajoRanking unidadTrabajo,
        IProveedorFechaHora reloj)
    {
        _repo = repo;
        _repoEventos = repoEventos;
        _unidadTrabajo = unidadTrabajo;
        _reloj = reloj;
    }

    public async Task Handle(
        ProcesarParticipanteUnidoComando comando, CancellationToken cancelacion)
    {
        if (await _repoEventos.ExisteAsync(comando.EventoId, TipoEvento, cancelacion))
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

            ranking.RegistrarParticipante(
                comando.ParticipanteSesionId,
                comando.ParticipanteIdentidadId,
                comando.EquipoId);

            await _repo.ActualizarAsync(ranking, ct);
            await _repoEventos.RegistrarAsync(comando.EventoId, TipoEvento, ahora, ct);
        }, cancelacion);
    }
}
