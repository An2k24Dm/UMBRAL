using Microsoft.AspNetCore.SignalR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos.TiempoReal;
using SesionesServicio.Infraestructura.TiempoReal.Hubs;

namespace SesionesServicio.Infraestructura.TiempoReal;

public sealed class NotificadorSesionesTiempoReal : INotificadorSesionesTiempoReal
{
    private readonly IHubContext<SesionesHub> _hub;

    public NotificadorSesionesTiempoReal(IHubContext<SesionesHub> hub)
    {
        _hub = hub;
    }

    public async Task NotificarParticipantesSesionActualizadosAsync(
        Guid sesionId,
        CancellationToken cancelacion)
    {
        var dto = new ParticipantesSesionActualizadosDto
        {
            SesionId = sesionId,
            FechaEventoUtc = DateTime.UtcNow
        };

        // Grupo de la sesión: refresca la pantalla de detalle del operador.
        await _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("ParticipantesSesionActualizados", dto, cancelacion);

        // Grupo del listado: actualiza la columna de participantes en la lista.
        await _hub.Clients
            .Group(SesionesHub.GrupoListadoSesiones)
            .SendAsync("ParticipantesSesionActualizados", dto, cancelacion);
    }

    public Task NotificarEquiposSesionActualizadosAsync(
        Guid sesionId,
        Guid? equipoId,
        CancellationToken cancelacion)
    {
        var dto = new EquiposSesionActualizadosDto
        {
            SesionId = sesionId,
            EquipoId = equipoId,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("EquiposSesionActualizados", dto, cancelacion);
    }

    public async Task NotificarEquipoActualizadoAsync(
        Guid sesionId,
        Guid equipoId,
        CancellationToken cancelacion)
    {
        var dto = new EquipoActualizadoTiempoRealDto
        {
            SesionId = sesionId,
            EquipoId = equipoId,
            FechaEventoUtc = DateTime.UtcNow
        };

        await _hub.Clients
            .Group(SesionesHub.GrupoEquipo(equipoId))
            .SendAsync("EquipoActualizado", dto, cancelacion);

        await _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("EquipoActualizado", dto, cancelacion);
    }

    public async Task NotificarSesionActualizadaAsync(
        Guid sesionId,
        string estado,
        CancellationToken cancelacion)
    {
        var dto = new SesionActualizadaTiempoRealDto
        {
            SesionId = sesionId,
            Estado = estado,
            FechaEventoUtc = DateTime.UtcNow
        };

        // Grupo de la sesión: refresca la pantalla de detalle abierta.
        await _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("SesionActualizada", dto, cancelacion);

        // Grupo del listado: refresca la lista de sesiones del operador.
        await _hub.Clients
            .Group(SesionesHub.GrupoListadoSesiones)
            .SendAsync("SesionActualizada", dto, cancelacion);
    }

    // HU44 — Aviso dirigido al participante expulsado. Usa el grupo de usuario
    // que resuelve IUserIdProvider con el mismo id que IUsuarioActual.ObtenerId.
    public Task NotificarParticipanteExpulsadoAsync(
        Guid participanteIdentidadId,
        Guid sesionId,
        Guid participanteSesionId,
        CancellationToken cancelacion)
    {
        var dto = new ParticipanteExpulsadoSesionDto
        {
            SesionId = sesionId,
            ParticipanteSesionId = participanteSesionId,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .User(participanteIdentidadId.ToString())
            .SendAsync("ParticipanteExpulsadoSesion", dto, cancelacion);
    }

    // HU44 — Aviso dirigido a cada integrante del equipo expulsado.
    public Task NotificarEquipoExpulsadoAsync(
        IReadOnlyCollection<Guid> participantesIdentidadIds,
        Guid sesionId,
        Guid equipoId,
        string equipoNombre,
        CancellationToken cancelacion)
    {
        if (participantesIdentidadIds.Count == 0)
            return Task.CompletedTask;

        var dto = new EquipoExpulsadoSesionDto
        {
            SesionId = sesionId,
            EquipoId = equipoId,
            EquipoNombre = equipoNombre,
            FechaEventoUtc = DateTime.UtcNow
        };

        var usuarios = participantesIdentidadIds
            .Select(id => id.ToString())
            .ToList();

        return _hub.Clients
            .Users(usuarios)
            .SendAsync("EquipoExpulsadoSesion", dto, cancelacion);
    }

    // HU-37 — Notifica al grupo de la sesión que un jugador respondió una pregunta.
    public Task NotificarRespuestaRegistradaAsync(
        Guid sesionId,
        Guid etapaId,
        Guid preguntaId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        bool esCorrecta,
        CancellationToken cancelacion)
    {
        var dto = new RespuestaRegistradaDto
        {
            SesionId = sesionId,
            EtapaId = etapaId,
            PreguntaId = preguntaId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            EsCorrecta = esCorrecta,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("RespuestaRegistrada", dto, cancelacion);
    }

    // HU-37 — Notifica que todos los jugadores completaron la etapa; el cliente avanza.
    public Task NotificarEtapaCompletadaAsync(
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        CancellationToken cancelacion)
    {
        var dto = new EtapaCompletadaDto
        {
            SesionId = sesionId,
            MisionId = misionId,
            EtapaId = etapaId,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("EtapaCompletada", dto, cancelacion);
    }

    // HU-50 — Notifica a todos los participantes de la sesión que el operador liberó una pista.
    public Task NotificarEtapaIniciadaAsync(
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        string tipoEtapa,
        Guid modoDeJuegoId,
        int ordenGlobal,
        DateTime fechaInicioEtapaUtc,
        int duracionSegundos,
        CancellationToken cancelacion)
    {
        var dto = new EtapaIniciadaDto
        {
            SesionId = sesionId,
            MisionId = misionId,
            EtapaId = etapaId,
            TipoEtapa = tipoEtapa,
            ModoDeJuegoId = modoDeJuegoId,
            OrdenGlobal = ordenGlobal,
            FechaInicioEtapaUtc = fechaInicioEtapaUtc,
            DuracionSegundos = duracionSegundos,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("EtapaIniciada", dto, cancelacion);
    }

    // HU37/HU49 — Aviso de preparación: la siguiente etapa comenzará en breve.
    public Task NotificarEtapaPorComenzarAsync(
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        string tipoEtapa,
        Guid modoDeJuegoId,
        int numeroMision,
        int numeroEtapa,
        int ordenGlobal,
        bool esNuevaMision,
        DateTime fechaInicioProgramadaUtc,
        int duracionPreparacionSegundos,
        CancellationToken cancelacion)
    {
        var dto = new EtapaPorComenzarDto
        {
            SesionId = sesionId,
            MisionId = misionId,
            EtapaId = etapaId,
            TipoEtapa = tipoEtapa,
            ModoDeJuegoId = modoDeJuegoId,
            NumeroMision = numeroMision,
            NumeroEtapa = numeroEtapa,
            OrdenGlobal = ordenGlobal,
            EsNuevaMision = esNuevaMision,
            FechaInicioProgramadaUtc = fechaInicioProgramadaUtc,
            DuracionPreparacionSegundos = duracionPreparacionSegundos,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("EtapaPorComenzar", dto, cancelacion);
    }

    public Task NotificarProgresoSecuencialActualizadoAsync(
        Guid sesionId,
        Guid participanteIdentidadId,
        Guid? equipoId,
        CancellationToken cancelacion)
    {
        var dto = new ProgresoSecuencialActualizadoDto
        {
            SesionId = sesionId,
            ParticipanteIdentidadId = participanteIdentidadId,
            EquipoId = equipoId,
            FechaEventoUtc = DateTime.UtcNow
        };

        var tareas = new List<Task>
        {
            _hub.Clients
                .Group(SesionesHub.GrupoSesion(sesionId))
                .SendAsync("ProgresoSecuencialActualizado", dto, cancelacion)
        };

        tareas.Add(equipoId.HasValue
            ? _hub.Clients
                .Group(SesionesHub.GrupoEquipo(equipoId.Value))
                .SendAsync("ProgresoSecuencialActualizado", dto, cancelacion)
            : _hub.Clients
                .User(participanteIdentidadId.ToString())
                .SendAsync("ProgresoSecuencialActualizado", dto, cancelacion));

        return Task.WhenAll(tareas);
    }

    public Task NotificarPistaLiberadaAsync(
        Guid sesionId,
        Guid etapaId,
        Guid? pistaId,
        string contenido,
        string tipo,
        double? latitud,
        double? longitud,
        CancellationToken cancelacion)
    {
        var dto = new PistaLiberadaDto
        {
            SesionId = sesionId,
            EtapaId = etapaId,
            PistaId = pistaId,
            Contenido = contenido,
            Tipo = tipo,
            Latitud = latitud,
            Longitud = longitud,
            FechaEventoUtc = DateTime.UtcNow
        };

        return _hub.Clients
            .Group(SesionesHub.GrupoSesion(sesionId))
            .SendAsync("PistaLiberada", dto, cancelacion);
    }
}
