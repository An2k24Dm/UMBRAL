using MediatR;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Comandos.ProcesarEquipoCreado;

public sealed class ProcesarEquipoCreadoManejador : IRequestHandler<ProcesarEquipoCreadoComando>
{
    private const string TipoEvento = "EquipoCreadoSesion";

    private readonly IRepositorioRankingEquipo _repo;
    private readonly IRepositorioEventosProcesados _repoEventos;
    private readonly IUnidadTrabajoRanking _unidadTrabajo;
    private readonly IProveedorFechaHora _reloj;

    public ProcesarEquipoCreadoManejador(
        IRepositorioRankingEquipo repo,
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
            var existente = await _repo.ObtenerPorSesionYEquipoAsync(
                comando.SesionId, comando.EquipoId, ct);

            if (existente is null)
            {
                var entrada = EntradaRankingEquipo.Crear(
                    comando.SesionId, comando.EquipoId, comando.NombreEquipo, ahora);
                await _repo.AgregarAsync(entrada, ct);
            }

            await _repoEventos.RegistrarAsync(comando.EventoId, TipoEvento, ahora, ct);
        }, cancelacion);
    }
}
