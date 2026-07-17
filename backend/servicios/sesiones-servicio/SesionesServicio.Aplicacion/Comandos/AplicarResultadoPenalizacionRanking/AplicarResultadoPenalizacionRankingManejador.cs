using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Comandos.AplicarResultadoPenalizacionRanking;

public sealed class AplicarResultadoPenalizacionRankingManejador
    : IRequestHandler<AplicarResultadoPenalizacionRankingComando>
{
    private const string TipoObjetivoEquipo = "Equipo";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IRepositorioPenalizacionesSesion _repositorioPenalizaciones;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly INotificadorSesionesTiempoReal _notificador;
    private readonly IProveedorFechaHora _reloj;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public AplicarResultadoPenalizacionRankingManejador(
        IRepositorioSesiones repositorio,
        IRepositorioPenalizacionesSesion repositorioPenalizaciones,
        IUnidadTrabajoSesiones unidadTrabajo,
        INotificadorSesionesTiempoReal notificador,
        IProveedorFechaHora reloj,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _repositorioPenalizaciones = repositorioPenalizaciones;
        _unidadTrabajo = unidadTrabajo;
        _notificador = notificador;
        _reloj = reloj;
        _registroLogs = registroLogs;
    }

    public async Task Handle(
        AplicarResultadoPenalizacionRankingComando comando, CancellationToken cancelacion)
    {
        var aplicado = false;

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var penalizacion = await _repositorioPenalizaciones.ObtenerPorEventoIdAsync(
                comando.EventoIdOrigen, ct);

            if (penalizacion is { EstadoProcesamiento: EstadoProcesamientoPenalizacion.Procesada })
                return;

            var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, ct);
            if (sesion is null)
                return;

            var snapshotActualizado = AplicarSnapshot(sesion, comando);
            if (snapshotActualizado)
                await _repositorio.ActualizarAsync(sesion, ct);

            if (penalizacion is not null)
            {
                var puntajeResultante = comando.TipoObjetivo == TipoObjetivoEquipo
                    ? comando.PuntajeTotalEquipo ?? 0L
                    : comando.PuntajeTotalParticipante ?? 0L;
                penalizacion.MarcarProcesada(puntajeResultante, _reloj.ObtenerFechaHoraUtc());
                await _repositorioPenalizaciones.ActualizarAsync(penalizacion, ct);
            }

            await _unidadTrabajo.GuardarCambiosAsync(ct);
            aplicado = true;
        }, cancelacion);

        if (!aplicado)
            return;

        if (comando.TipoObjetivo == TipoObjetivoEquipo && comando.EquipoId is Guid equipoId)
            await _notificador.NotificarEquipoActualizadoAsync(comando.SesionId, equipoId, cancelacion);

        await _notificador.NotificarParticipantesSesionActualizadosAsync(
            comando.SesionId, cancelacion);

        _registroLogs.Informacion(
            evento: "PenalizacionProcesadaAplicada",
            descripcion: "Sesiones aplicó el resultado de una penalización y actualizó su snapshot",
            propiedades: new Dictionary<string, object?>
            {
                ["EventoId"] = comando.EventoIdOrigen,
                ["PenalizacionId"] = comando.PenalizacionId,
                ["SesionId"] = comando.SesionId,
                ["TipoObjetivo"] = comando.TipoObjetivo,
                ["PuntosPenalizadosAcumulados"] = comando.PuntosPenalizadosAcumulados
            });
    }

    private static bool AplicarSnapshot(
        Sesion sesion, AplicarResultadoPenalizacionRankingComando comando)
    {
        if (sesion is SesionIndividual individual
            && comando.TipoObjetivo != TipoObjetivoEquipo
            && comando.ParticipanteSesionId is Guid participanteSesionId)
        {
            var participante = individual.Participantes
                .FirstOrDefault(p => p.Id == participanteSesionId);
            var puntaje = checked((int)(comando.PuntajeTotalParticipante ?? 0L));
            return participante?.EstablecerPenalizacionSnapshot(
                comando.PuntosPenalizadosAcumulados, puntaje, comando.CalculadoEnUtc) == true;
        }

        if (sesion is SesionGrupal grupal
            && comando.TipoObjetivo == TipoObjetivoEquipo
            && comando.EquipoId is Guid equipoId)
        {
            var equipo = grupal.Equipos.FirstOrDefault(e => e.Id == equipoId);
            var puntaje = checked((int)(comando.PuntajeTotalEquipo ?? 0L));
            return equipo?.EstablecerPenalizacionSnapshot(
                comando.PuntosPenalizadosAcumulados, puntaje, comando.CalculadoEnUtc) == true;
        }

        return false;
    }
}
