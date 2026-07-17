using MediatR;
using Microsoft.Extensions.Logging;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Commons.Dtos.Eventos.Salida;
using RankingServicio.Commons.Dtos.TiempoReal;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Dominio.Excepciones;
using RankingServicio.Dominio.ObjetosValor;

namespace RankingServicio.Aplicacion.Comandos.ProcesarPenalizacion;

public sealed class ProcesarPenalizacionManejador
    : IRequestHandler<ProcesarPenalizacionComando>
{
    private const string TipoEvento = "PenalizacionAplicada";
    private const string ObjetivoParticipante = "Participante";
    private const string ObjetivoEquipo = "Equipo";

    private readonly IRepositorioRanking _repo;
    private readonly IRepositorioEventosProcesados _repoEventos;
    private readonly IUnidadTrabajoRanking _unidadTrabajo;
    private readonly INotificadorRankingTiempoReal _notificador;
    private readonly IPublicadorResultadosPuntaje _publicadorResultados;
    private readonly IProveedorFechaHora _reloj;
    private readonly ILogger<ProcesarPenalizacionManejador> _log;

    public ProcesarPenalizacionManejador(
        IRepositorioRanking repo,
        IRepositorioEventosProcesados repoEventos,
        IUnidadTrabajoRanking unidadTrabajo,
        INotificadorRankingTiempoReal notificador,
        IPublicadorResultadosPuntaje publicadorResultados,
        IProveedorFechaHora reloj,
        ILogger<ProcesarPenalizacionManejador> log)
    {
        _repo = repo;
        _repoEventos = repoEventos;
        _unidadTrabajo = unidadTrabajo;
        _notificador = notificador;
        _publicadorResultados = publicadorResultados;
        _reloj = reloj;
        _log = log;
    }

    public async Task Handle(ProcesarPenalizacionComando comando, CancellationToken cancelacion)
    {
        if (await _repoEventos.ExisteAsync(comando.EventoId, TipoEvento, cancelacion))
            return;

        var cantidad = CantidadPenalizacion.Crear(comando.Puntos);
        var resultado = await AplicarPenalizacionAsync(comando, cantidad, cancelacion);

        _log.LogInformation(
            "Penalización aplicada en Ranking. EventoId={EventoId} SesionId={SesionId} TipoObjetivo={TipoObjetivo} ObjetivoId={ObjetivoId} PuntosDescontados={Puntos} PuntajeNuevo={PuntajeNuevo} TotalPenalizadoAcumulado={TotalPenalizado}",
            comando.EventoId,
            comando.SesionId,
            comando.TipoObjetivo,
            comando.TipoObjetivo == ObjetivoEquipo ? comando.EquipoId : comando.ParticipanteSesionId,
            comando.Puntos,
            resultado.Notificacion.PuntajeResultante,
            resultado.Notificacion.PuntosPenalizadosAcumulados);

        if (comando.TipoObjetivo == ObjetivoEquipo)
            await _notificador.NotificarRankingEquiposActualizadoAsync(comando.SesionId, cancelacion);
        else
            await _notificador.NotificarRankingParticipantesActualizadoAsync(comando.SesionId, cancelacion);

        await _notificador.NotificarPenalizacionAplicadaAsync(resultado.Notificacion, cancelacion);
    }

    private async Task<ResultadoPenalizacion> AplicarPenalizacionAsync(
        ProcesarPenalizacionComando comando,
        CantidadPenalizacion cantidad,
        CancellationToken cancelacion)
    {
        ResultadoPenalizacion? resultado = null;
        var ahora = _reloj.ObtenerFechaHoraUtc();

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            var ranking = await _repo.ObtenerPorSesionAsync(comando.SesionId, ct);
            if (ranking is null)
            {
                ranking = Ranking.Crear(comando.SesionId);
                await _repo.AgregarAsync(ranking, ct);
            }

            resultado = comando.TipoObjetivo == ObjetivoEquipo
                ? AplicarAEquipo(ranking, comando, cantidad, ahora)
                : AplicarAParticipante(ranking, comando, cantidad, ahora);

            await _repo.ActualizarAsync(ranking, ct);
            await _repoEventos.RegistrarAsync(comando.EventoId, TipoEvento, ahora, ct);
            await _publicadorResultados.PublicarPenalizacionProcesadaAsync(resultado.Resultado, ct);
        }, cancelacion);

        return resultado!;
    }

    private static ResultadoPenalizacion AplicarAParticipante(
        Ranking ranking,
        ProcesarPenalizacionComando comando,
        CantidadPenalizacion cantidad,
        DateTime ahora)
    {
        if (comando.ParticipanteSesionId is not Guid participanteSesionId
            || comando.ParticipanteIdentidadId is not Guid participanteIdentidadId)
            throw new RankingInvalidoExcepcion(
                "La penalización de participante requiere ParticipanteSesionId y ParticipanteIdentidadId.");

        var participante = ranking.AplicarPenalizacionParticipante(
            participanteSesionId, participanteIdentidadId, cantidad);

        var resultado = new PenalizacionProcesadaDto(
            comando.EventoId,
            comando.SesionId,
            ObjetivoParticipante,
            participanteSesionId,
            participanteIdentidadId,
            null,
            cantidad.Valor,
            participante.PuntosPenalizados,
            participante.Puntaje.Valor,
            null,
            ahora);

        var notificacion = new PenalizacionAplicadaNotificacionDto(
            comando.SesionId,
            ObjetivoParticipante,
            participanteSesionId,
            cantidad.Valor,
            participante.PuntosPenalizados,
            participante.Puntaje.Valor,
            ahora);

        return new ResultadoPenalizacion(resultado, notificacion);
    }

    private static ResultadoPenalizacion AplicarAEquipo(
        Ranking ranking,
        ProcesarPenalizacionComando comando,
        CantidadPenalizacion cantidad,
        DateTime ahora)
    {
        if (comando.EquipoId is not Guid equipoId)
            throw new RankingInvalidoExcepcion(
                "La penalización de equipo requiere EquipoId.");

        var equipo = ranking.AplicarPenalizacionEquipo(equipoId, cantidad);

        var resultado = new PenalizacionProcesadaDto(
            comando.EventoId,
            comando.SesionId,
            ObjetivoEquipo,
            null,
            null,
            equipoId,
            cantidad.Valor,
            equipo.PuntosPenalizados,
            null,
            equipo.Puntaje.Valor,
            ahora);

        var notificacion = new PenalizacionAplicadaNotificacionDto(
            comando.SesionId,
            ObjetivoEquipo,
            equipoId,
            cantidad.Valor,
            equipo.PuntosPenalizados,
            equipo.Puntaje.Valor,
            ahora);

        return new ResultadoPenalizacion(resultado, notificacion);
    }

    private sealed record ResultadoPenalizacion(
        PenalizacionProcesadaDto Resultado,
        PenalizacionAplicadaNotificacionDto Notificacion);
}
