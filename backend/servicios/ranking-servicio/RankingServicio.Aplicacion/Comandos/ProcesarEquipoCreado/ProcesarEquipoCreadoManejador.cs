using MediatR;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Comandos.ProcesarEquipoCreado;

public sealed class ProcesarEquipoCreadoManejador : IRequestHandler<ProcesarEquipoCreadoComando>
{
    private const string TipoEvento = "EquipoCreadoSesion";

    private readonly IRepositorioRanking _repo;
    private readonly IRepositorioEventosProcesados _repoEventos;
    private readonly IUnidadTrabajoRanking _unidadTrabajo;
    private readonly IProveedorFechaHora _reloj;

    public ProcesarEquipoCreadoManejador(
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
        ProcesarEquipoCreadoComando comando, CancellationToken cancelacion)
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

            ranking.RegistrarEquipo(comando.EquipoId);

            await _repo.ActualizarAsync(ranking, ct);
            await _repoEventos.RegistrarAsync(comando.EventoId, TipoEvento, ahora, ct);
        }, cancelacion);
    }
}
