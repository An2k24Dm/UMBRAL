using MediatR;
using SesionesServicio.Aplicacion.Excepciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.EnviarEvidenciaTesoro;

public sealed class EnviarEvidenciaTesoroManejador
    : IRequestHandler<EnviarEvidenciaTesoroComando, EvidenciaTesoroRespuestaDto>
{
    private readonly IUsuarioActual _usuario;
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly IClienteBusquedaTesoro _clienteTesoro;
    private readonly IRepositorioEvidenciasTesoro _repositorioEvidencias;
    private readonly INotificadorSesionesTiempoReal _notificador;
    private readonly IServicioFinalizacionSesion _servicioFinalizacion;
    private readonly IServicioProgresoSecuencialSesion _servicioProgresoSecuencial;
    private readonly IPublicadorEventosRanking _publicadorRanking;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;

    public EnviarEvidenciaTesoroManejador(
        IUsuarioActual usuario,
        IRepositorioSesiones repositorioSesiones,
        IClienteBusquedaTesoro clienteTesoro,
        IRepositorioEvidenciasTesoro repositorioEvidencias,
        INotificadorSesionesTiempoReal notificador,
        IServicioFinalizacionSesion servicioFinalizacion,
        IServicioProgresoSecuencialSesion servicioProgresoSecuencial,
        IPublicadorEventosRanking publicadorRanking,
        IUnidadTrabajoSesiones unidadTrabajo)
    {
        _usuario = usuario;
        _repositorioSesiones = repositorioSesiones;
        _clienteTesoro = clienteTesoro;
        _repositorioEvidencias = repositorioEvidencias;
        _notificador = notificador;
        _servicioFinalizacion = servicioFinalizacion;
        _servicioProgresoSecuencial = servicioProgresoSecuencial;
        _publicadorRanking = publicadorRanking;
        _unidadTrabajo = unidadTrabajo;
    }

    public async Task<EvidenciaTesoroRespuestaDto> Handle(
        EnviarEvidenciaTesoroComando comando, CancellationToken cancelacion)
    {
        var participanteId = _usuario.ObtenerId()
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesion solicitada no existe.");

        if (sesion.Estado != EstadoSesion.Activa)
            throw new OperacionSesionInvalidaExcepcion(
                $"La sesion no esta activa. Estado actual: {sesion.Estado}.");

        var (participante, totalJugadoresEsperados) = ObtenerJugador(sesion, participanteId);
        var equipoId = participante.EquipoId;

        await _servicioProgresoSecuencial.ValidarEtapaActualAsync(
            sesion,
            participanteId,
            comando.MisionId,
            comando.EtapaId,
            "BusquedaTesoro",
            comando.BusquedaId,
            cancelacion);

        var yaCompletado = equipoId.HasValue
            ? await _repositorioEvidencias.ExisteEvidenciaValidaEquipoAsync(
                comando.SesionId, comando.EtapaId, equipoId.Value, cancelacion)
            : await _repositorioEvidencias.ExisteEvidenciaValidaIndividualAsync(
                comando.SesionId, comando.EtapaId, participanteId, cancelacion);

        if (yaCompletado)
            throw new EvidenciaTesoroDuplicadaExcepcion(esEquipo: equipoId.HasValue);

        var esValida = await _clienteTesoro.ValidarCodigoQrAsync(
            comando.BusquedaId, comando.CodigoEscaneado, cancelacion)
            ?? throw new InvalidOperationException("Búsqueda del tesoro no encontrada.");

        var busqueda = await _clienteTesoro.ObtenerBusquedaParticipanteAsync(
            comando.BusquedaId, cancelacion)
            ?? throw new InvalidOperationException("Búsqueda del tesoro no encontrada.");
        var eventoId = Guid.NewGuid();

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
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
                FechaEnvioUtc: DateTime.UtcNow),
                ct);

            await _publicadorRanking.PublicarEvidenciaTesoroRegistradaAsync(
                eventoId,
                comando.SesionId, comando.MisionId, comando.EtapaId,
                participante.Id, participanteId,
                equipoId, comando.BusquedaId, esValida,
                busqueda.Puntaje, ct);
        }, cancelacion);

        var etapaCompletada = false;
        if (esValida)
        {
            await _notificador.NotificarProgresoSecuencialActualizadoAsync(
                comando.SesionId, participanteId, equipoId, cancelacion);

            var completados = equipoId.HasValue
                ? await _repositorioEvidencias.ContarEquiposConEvidenciaValidaAsync(
                    comando.SesionId, comando.EtapaId, cancelacion)
                : await _repositorioEvidencias.ContarParticipantesConEvidenciaValidaAsync(
                    comando.SesionId, comando.EtapaId, cancelacion);

            if (completados >= totalJugadoresEsperados)
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

    private static (Participante participante, int totalJugadores) ObtenerJugador(
        Sesion sesion, Guid participanteId)
    {
        if (sesion is SesionIndividual individual)
        {
            var p = individual.Participantes
                .FirstOrDefault(x => x.ParticipanteIdentidadId == participanteId)
                ?? throw new ParticipacionInvalidaExcepcion(
                    "El participante no esta inscrito en esta sesion.");
            return (p, individual.Participantes.Count);
        }

        if (sesion is SesionGrupal grupal)
        {
            foreach (var equipo in grupal.Equipos)
            {
                var p = equipo.Participantes
                    .FirstOrDefault(x => x.ParticipanteIdentidadId == participanteId);
                if (p is not null)
                    return (p, grupal.Equipos.Count);
            }

            throw new ParticipacionInvalidaExcepcion(
                "El participante no esta inscrito en esta sesion.");
        }

        throw new SesionInvalidaExcepcion("Tipo de sesion no soportado.");
    }
}
