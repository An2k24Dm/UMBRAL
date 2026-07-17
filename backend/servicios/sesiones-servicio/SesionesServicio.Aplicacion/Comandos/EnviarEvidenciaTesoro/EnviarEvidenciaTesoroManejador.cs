using MediatR;
using SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Aplicacion.Comandos.EnviarEvidenciaTesoro;

public sealed class EnviarEvidenciaTesoroManejador
    : IRequestHandler<EnviarEvidenciaTesoroComando, EvidenciaTesoroRespuestaDto>
{
    private readonly IUsuarioActual _usuario;
    private readonly FabricaCadenaValidacionEvidenciaTesoro _fabricaCadena;
    private readonly IClienteBusquedaTesoro _clienteTesoro;
    private readonly IRepositorioEvidenciasTesoro _repositorioEvidencias;
    private readonly INotificadorSesionesTiempoReal _notificador;
    private readonly IServicioFinalizacionSesion _servicioFinalizacion;
    private readonly IPublicadorEventosRanking _publicadorRanking;
    private readonly IProveedorFechaHora _reloj;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;

    public EnviarEvidenciaTesoroManejador(
        IUsuarioActual usuario,
        FabricaCadenaValidacionEvidenciaTesoro fabricaCadena,
        IClienteBusquedaTesoro clienteTesoro,
        IRepositorioEvidenciasTesoro repositorioEvidencias,
        INotificadorSesionesTiempoReal notificador,
        IServicioFinalizacionSesion servicioFinalizacion,
        IPublicadorEventosRanking publicadorRanking,
        IProveedorFechaHora reloj,
        IUnidadTrabajoSesiones unidadTrabajo)
    {
        _usuario = usuario;
        _fabricaCadena = fabricaCadena;
        _clienteTesoro = clienteTesoro;
        _repositorioEvidencias = repositorioEvidencias;
        _notificador = notificador;
        _servicioFinalizacion = servicioFinalizacion;
        _publicadorRanking = publicadorRanking;
        _reloj = reloj;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<EvidenciaTesoroRespuestaDto> Handle(
        EnviarEvidenciaTesoroComando comando, CancellationToken cancelacion)
    {
        var participanteId = _usuario.ObtenerId()
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var contexto = new ContextoValidacionEvidenciaTesoro
        {
            SesionId = comando.SesionId,
            ParticipanteIdentidadId = participanteId,
            MisionId = comando.MisionId,
            EtapaId = comando.EtapaId,
            BusquedaId = comando.BusquedaId,
            CodigoEscaneado = comando.CodigoEscaneado
        };

        var cadena = _fabricaCadena.Crear();
        await cadena.ManejarAsync(contexto, cancelacion);

        var sesion = contexto.Sesion!;
        var participante = contexto.Participante!;
        var equipoId = contexto.EquipoId;
        var totalJugadoresEsperados = contexto.TotalCompetidores;
        var esValida = contexto.EsCodigoQrValido;

        var busqueda = await _clienteTesoro.ObtenerBusquedaParticipanteAsync(
            comando.BusquedaId, cancelacion)
            ?? throw new InvalidOperationException("Búsqueda del tesoro no encontrada.");

        var ejecucion = sesion.EjecucionActual
            ?? throw new OperacionSesionInvalidaExcepcion(
                "La sesion no tiene una etapa activa.");
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        var tiempoLimiteMs = (int)Math.Min(
            (long)ejecucion.DuracionSegundos * 1000L, int.MaxValue);
        var tiempoTranscurridoMs = (int)Math.Clamp(
            ejecucion.CalcularTiempoActivoTranscurridoMs(ahoraUtc), 0L, tiempoLimiteMs);

        if (ejecucion.CalcularSegundosRestantes(ahoraUtc) <= 0)
            throw new OperacionSesionInvalidaExcepcion(
                "El tiempo de la etapa ya finalizó.");

        var eventoId = Guid.NewGuid();
        var ordenResolucion = 0;

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            if (esValida)
                await _repositorioEvidencias.BloquearEtapaParaOrdenAsync(
                    comando.SesionId, comando.EtapaId, ct);

            await _repositorioEvidencias.AgregarAsync(new EvidenciaTesoroRegistro(
                SesionId: comando.SesionId,
                MisionId: comando.MisionId,
                EtapaId: comando.EtapaId,
                BusquedaId: comando.BusquedaId,
                ParticipanteIdentidadId: participanteId,
                EquipoId: equipoId,
                CodigoEnviado: comando.CodigoEscaneado,
                EsValida: esValida,
                PuntosGanados: 0,
                EventoPuntuacionId: eventoId,
                FechaEnvioUtc: ahoraUtc),
                ct);

            if (esValida)
                ordenResolucion = equipoId.HasValue
                    ? await _repositorioEvidencias.ContarEquiposConEvidenciaValidaAsync(
                        comando.SesionId, comando.EtapaId, ct)
                    : await _repositorioEvidencias.ContarParticipantesConEvidenciaValidaAsync(
                        comando.SesionId, comando.EtapaId, ct);

            await _publicadorRanking.PublicarEvidenciaTesoroRegistradaAsync(
                eventoId,
                comando.SesionId, comando.MisionId, comando.EtapaId,
                participante.Id, participanteId,
                equipoId, comando.BusquedaId, esValida,
                busqueda.Puntaje, ordenResolucion, totalJugadoresEsperados,
                tiempoTranscurridoMs, tiempoLimiteMs, ct);
        }, cancelacion);

        var etapaCompletada = false;
        if (esValida)
        {
            await _notificador.NotificarProgresoSecuencialActualizadoAsync(
                comando.SesionId, participanteId, equipoId, cancelacion);

            if (ordenResolucion >= totalJugadoresEsperados)
            {
                etapaCompletada = true;
                await _servicioFinalizacion.ProgramarCierreTrasFeedbackAsync(
                    comando.SesionId, comando.EtapaId, cancelacion);
            }
        }

        return new EvidenciaTesoroRespuestaDto
        {
            EsValida = esValida,
            EventoId = eventoId,
            EtapaCompletada = etapaCompletada
        };
    }
}
