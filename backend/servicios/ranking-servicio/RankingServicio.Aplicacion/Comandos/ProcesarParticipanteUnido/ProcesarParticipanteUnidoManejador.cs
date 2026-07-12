using MediatR;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Comandos.ProcesarParticipanteUnido;

public sealed class ProcesarParticipanteUnidoManejador
    : IRequestHandler<ProcesarParticipanteUnidoComando>
{
    private const string TipoEvento = "ParticipanteUnidoSesion";

    private readonly IRepositorioRankingParticipante _repo;
    private readonly IRepositorioEventosProcesados _repoEventos;
    private readonly IUnidadTrabajoRanking _unidadTrabajo;
    private readonly IProveedorFechaHora _reloj;

    public ProcesarParticipanteUnidoManejador(
        IRepositorioRankingParticipante repo,
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
            var existente = await _repo.ObtenerPorSesionYParticipanteAsync(
                comando.SesionId, comando.ParticipanteIdentidadId, ct);

            if (existente is null)
            {
                var entrada = EntradaRankingParticipante.Crear(
                    comando.SesionId, comando.ParticipanteIdentidadId,
                    comando.NombreParticipante, ahora);
                await _repo.AgregarAsync(entrada, ct);
            }

            await _repoEventos.RegistrarAsync(comando.EventoId, TipoEvento, ahora, ct);
        }, cancelacion);
    }
}
