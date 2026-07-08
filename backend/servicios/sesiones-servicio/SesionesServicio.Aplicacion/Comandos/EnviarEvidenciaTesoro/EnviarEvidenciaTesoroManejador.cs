using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Comandos.EnviarEvidenciaTesoro;

public sealed class EnviarEvidenciaTesoroManejador
    : IRequestHandler<EnviarEvidenciaTesoroComando, EvidenciaTesoroRespuestaDto>
{
    private readonly IUsuarioActual _usuario;
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly IClienteBusquedaTesoro _clienteTesoro;
    private readonly IRepositorioEvidenciasTesoro _repositorioEvidencias;
    private readonly INotificadorSesionesTiempoReal _notificador;

    public EnviarEvidenciaTesoroManejador(
        IUsuarioActual usuario,
        IRepositorioSesiones repositorioSesiones,
        IClienteBusquedaTesoro clienteTesoro,
        IRepositorioEvidenciasTesoro repositorioEvidencias,
        INotificadorSesionesTiempoReal notificador)
    {
        _usuario = usuario;
        _repositorioSesiones = repositorioSesiones;
        _clienteTesoro = clienteTesoro;
        _repositorioEvidencias = repositorioEvidencias;
        _notificador = notificador;
    }

    public async Task<EvidenciaTesoroRespuestaDto> Handle(
        EnviarEvidenciaTesoroComando comando, CancellationToken cancelacion)
    {
        var participanteId = _usuario.ObtenerId()
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new InvalidOperationException("Sesión no encontrada.");

        // STATE PATTERN: el sistema bloquea el envío si el estado de la sesión no es Activa.
        if (sesion.Estado != EstadoSesion.Activa)
            throw new InvalidOperationException($"La sesión no está activa. Estado actual: {sesion.Estado}.");

        // Verificar que el participante está inscrito en la sesión.
        var totalJugadores = ObtenerTotalJugadores(sesion, participanteId);

        // Idempotencia: no permitir enviar evidencia si ya envió una (válida o no).
        var yaEnvio = await _repositorioEvidencias.ExisteEvidenciaAsync(
            comando.SesionId, comando.EtapaId, participanteId, cancelacion);
        if (yaEnvio)
            throw new InvalidOperationException("Ya enviaste una evidencia para esta etapa.");

        // Validar el código QR escaneado contra juegos-servicio.
        // El endpoint de validación acepta cualquier token autenticado y solo devuelve bool.
        var esValida = await _clienteTesoro.ValidarCodigoQrAsync(
            comando.BusquedaId, comando.CodigoEscaneado, cancelacion)
            ?? throw new InvalidOperationException("Búsqueda del tesoro no encontrada.");

        // Calcular puntaje: solo si es válida (puntaje base de la búsqueda).
        var puntosGanados = 0;
        if (esValida)
        {
            var busqueda = await _clienteTesoro.ObtenerBusquedaParticipanteAsync(
                comando.BusquedaId, cancelacion);
            puntosGanados = busqueda?.Puntaje ?? 0;
        }

        await _repositorioEvidencias.AgregarAsync(new EvidenciaTesoroRegistro(
            SesionId: comando.SesionId,
            MisionId: comando.MisionId,
            EtapaId: comando.EtapaId,
            BusquedaId: comando.BusquedaId,
            ParticipanteIdentidadId: participanteId,
            CodigoEnviado: comando.CodigoEscaneado,
            EsValida: esValida,
            PuntosGanados: puntosGanados,
            FechaEnvioUtc: DateTime.UtcNow),
            cancelacion);

        // Comprobar si todos los jugadores enviaron evidencia válida.
        var etapaCompletada = false;
        if (esValida)
        {
            var conEvidenciaValida = await _repositorioEvidencias
                .ContarParticipantesConEvidenciaValidaAsync(
                    comando.SesionId, comando.EtapaId, cancelacion);

            if (conEvidenciaValida >= totalJugadores)
            {
                etapaCompletada = true;
                await _notificador.NotificarEtapaCompletadaAsync(
                    comando.SesionId, comando.MisionId, comando.EtapaId, cancelacion);
            }
        }

        return new EvidenciaTesoroRespuestaDto
        {
            EsValida = esValida,
            PuntosGanados = puntosGanados,
            EtapaCompletada = etapaCompletada
        };
    }

    // Valida que el participante está en la sesión y retorna el total de jugadores esperados.
    private static int ObtenerTotalJugadores(Sesion sesion, Guid participanteId)
    {
        if (sesion is SesionIndividual individual)
        {
            if (!individual.Participantes.Any(p => p.ParticipanteIdentidadId == participanteId))
                throw new InvalidOperationException(
                    "El participante no está inscrito en esta sesión.");
            return individual.Participantes.Count;
        }

        if (sesion is SesionGrupal grupal)
        {
            var equipo = grupal.Equipos
                .FirstOrDefault(e => e.Participantes.Any(
                    p => p.ParticipanteIdentidadId == participanteId));
            if (equipo is null)
                throw new InvalidOperationException(
                    "El participante no está inscrito en esta sesión.");
            return grupal.Equipos.Count;
        }

        throw new InvalidOperationException("Tipo de sesión no soportado.");
    }
}
