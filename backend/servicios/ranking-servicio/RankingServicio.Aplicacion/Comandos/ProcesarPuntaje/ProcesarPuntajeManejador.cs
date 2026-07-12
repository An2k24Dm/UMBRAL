using MediatR;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Aplicacion.Comandos.ProcesarPuntaje;

public sealed class ProcesarPuntajeManejador : IRequestHandler<ProcesarPuntajeComando>
{
    private readonly IRepositorioRankingParticipante _repoParticipante;
    private readonly IRepositorioRankingEquipo _repoEquipo;
    private readonly IRepositorioEventosProcesados _repoEventos;
    private readonly IUnidadTrabajoRanking _unidadTrabajo;
    private readonly INotificadorRankingTiempoReal _notificador;
    private readonly IProveedorFechaHora _reloj;

    public ProcesarPuntajeManejador(
        IRepositorioRankingParticipante repoParticipante,
        IRepositorioRankingEquipo repoEquipo,
        IRepositorioEventosProcesados repoEventos,
        IUnidadTrabajoRanking unidadTrabajo,
        INotificadorRankingTiempoReal notificador,
        IProveedorFechaHora reloj)
    {
        _repoParticipante = repoParticipante;
        _repoEquipo = repoEquipo;
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
            await ActualizarParticipanteAsync(comando, ahora, ct);

            if (comando.EquipoId.HasValue && !string.IsNullOrWhiteSpace(comando.NombreEquipo))
                await ActualizarEquipoAsync(comando, ahora, ct);

            await _repoEventos.RegistrarAsync(comando.EventoId, clave, ahora, ct);
        }, cancelacion);

        await _notificador.NotificarRankingParticipantesActualizadoAsync(comando.SesionId, cancelacion);

        if (comando.EquipoId.HasValue)
            await _notificador.NotificarRankingEquiposActualizadoAsync(comando.SesionId, cancelacion);
    }

    private async Task ActualizarParticipanteAsync(
        ProcesarPuntajeComando cmd, DateTime ahora, CancellationToken ct)
    {
        var entrada = await _repoParticipante.ObtenerPorSesionYParticipanteAsync(
            cmd.SesionId, cmd.ParticipanteIdentidadId, ct);

        if (entrada is null)
        {
            entrada = EntradaRankingParticipante.Crear(
                cmd.SesionId, cmd.ParticipanteIdentidadId, cmd.NombreParticipante, ahora);
            entrada.AgregarPuntaje(cmd.Puntaje, cmd.EsCorrecta, ahora);
            await _repoParticipante.AgregarAsync(entrada, ct);
        }
        else
        {
            entrada.AgregarPuntaje(cmd.Puntaje, cmd.EsCorrecta, ahora);
            await _repoParticipante.ActualizarAsync(entrada, ct);
        }

        await RecalcularPosicionesParticipantesAsync(cmd.SesionId, ct);
    }

    private async Task ActualizarEquipoAsync(
        ProcesarPuntajeComando cmd, DateTime ahora, CancellationToken ct)
    {
        var entrada = await _repoEquipo.ObtenerPorSesionYEquipoAsync(
            cmd.SesionId, cmd.EquipoId!.Value, ct);

        if (entrada is null)
        {
            entrada = EntradaRankingEquipo.Crear(
                cmd.SesionId, cmd.EquipoId.Value, cmd.NombreEquipo!, ahora);
            entrada.AgregarPuntaje(cmd.Puntaje, ahora);
            await _repoEquipo.AgregarAsync(entrada, ct);
        }
        else
        {
            entrada.AgregarPuntaje(cmd.Puntaje, ahora);
            await _repoEquipo.ActualizarAsync(entrada, ct);
        }

        await RecalcularPosicionesEquiposAsync(cmd.SesionId, ct);
    }

    private async Task RecalcularPosicionesParticipantesAsync(
        Guid sesionId, CancellationToken ct)
    {
        var entradas = await _repoParticipante.ObtenerPorSesionAsync(sesionId, ct);
        var ordenadas = entradas.OrderByDescending(e => e.PuntajeTotal).ToList();
        for (var i = 0; i < ordenadas.Count; i++)
        {
            ordenadas[i].ActualizarPosicion(i + 1);
            await _repoParticipante.ActualizarAsync(ordenadas[i], ct);
        }
    }

    private async Task RecalcularPosicionesEquiposAsync(Guid sesionId, CancellationToken ct)
    {
        var entradas = await _repoEquipo.ObtenerPorSesionAsync(sesionId, ct);
        var ordenadas = entradas.OrderByDescending(e => e.PuntajeTotal).ToList();
        for (var i = 0; i < ordenadas.Count; i++)
        {
            ordenadas[i].ActualizarPosicion(i + 1);
            await _repoEquipo.ActualizarAsync(ordenadas[i], ct);
        }
    }
}
