using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Comandos.AplicarPuntajeRanking;

public sealed class AplicarPuntajeRankingManejador
    : IRequestHandler<AplicarPuntajeRankingComando>
{
    private readonly IRepositorioSesiones _repositorio;
    private readonly IRepositorioRespuestasTrivia _respuestasTrivia;
    private readonly IRepositorioEvidenciasTesoro _evidenciasTesoro;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly INotificadorSesionesTiempoReal _notificador;

    public AplicarPuntajeRankingManejador(
        IRepositorioSesiones repositorio,
        IRepositorioRespuestasTrivia respuestasTrivia,
        IRepositorioEvidenciasTesoro evidenciasTesoro,
        IUnidadTrabajoSesiones unidadTrabajo,
        INotificadorSesionesTiempoReal notificador)
    {
        _repositorio = repositorio;
        _respuestasTrivia = respuestasTrivia;
        _evidenciasTesoro = evidenciasTesoro;
        _unidadTrabajo = unidadTrabajo;
        _notificador = notificador;
    }

    public async Task Handle(AplicarPuntajeRankingComando comando, CancellationToken cancelacion)
    {
        var actualizado = false;
        var puntos = checked((int)comando.PuntajeGanado);

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            // Correlación: fija el puntaje REAL (calculado por ranking) en la
            // respuesta/evidencia original identificada por EventoPuntuacionId.
            // Solo una de las dos coincide; la otra es no-op. Idempotente.
            await _respuestasTrivia.ActualizarPuntosGanadosPorEventoAsync(
                comando.EventoIdOrigen, puntos, ct);
            await _evidenciasTesoro.ActualizarPuntosGanadosPorEventoAsync(
                comando.EventoIdOrigen, puntos, ct);

            var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, ct);
            if (sesion is null)
                return;

            actualizado = AplicarSnapshot(sesion, comando);
            if (actualizado)
                await _repositorio.ActualizarAsync(sesion, ct);
        }, cancelacion);

        if (!actualizado)
            return;

        if (comando.EquipoId.HasValue)
            await _notificador.NotificarEquipoActualizadoAsync(
                comando.SesionId, comando.EquipoId.Value, cancelacion);

        await _notificador.NotificarParticipantesSesionActualizadosAsync(
            comando.SesionId, cancelacion);
    }

    private static bool AplicarSnapshot(Sesion sesion, AplicarPuntajeRankingComando comando)
    {
        var puntajeParticipante = checked((int)comando.PuntajeTotalParticipante);
        var puntajeEquipo = comando.PuntajeTotalEquipo.HasValue
            ? checked((int)comando.PuntajeTotalEquipo.Value)
            : (int?)null;

        if (sesion is SesionIndividual individual)
        {
            var participante = individual.Participantes.FirstOrDefault(p =>
                p.Id == comando.ParticipanteSesionId);
            return participante?.EstablecerPuntajeSnapshot(
                puntajeParticipante, comando.CalculadoEnUtc) == true;
        }

        if (sesion is SesionGrupal grupal && comando.EquipoId.HasValue)
        {
            var equipo = grupal.Equipos.FirstOrDefault(e => e.Id == comando.EquipoId.Value);
            return equipo?.EstablecerPuntajeSnapshotParticipante(
                comando.ParticipanteSesionId,
                puntajeParticipante,
                puntajeEquipo,
                comando.CalculadoEnUtc) == true;
        }

        return false;
    }
}
