using Microsoft.AspNetCore.SignalR;
using SesionesServicio.Commons.Dtos.TiempoReal;
using SesionesServicio.Infraestructura.TiempoReal;
using SesionesServicio.Infraestructura.TiempoReal.Hubs;

namespace SesionesServicio.PruebasUnitarias.TiempoReal;

public sealed class NotificadorSesionesTiempoRealPruebas
{
    [Fact]
    public async Task ParticipantesSesionActualizados_NotificaSesionYListado()
    {
        var sesionId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarParticipantesSesionActualizadosAsync(
            sesionId, CancellationToken.None);

        arranque.Clients.Verify(c => c.Group(SesionesHub.GrupoSesion(sesionId)), Times.Once);
        arranque.Clients.Verify(c => c.Group(SesionesHub.GrupoListadoSesiones), Times.Once);
        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "ParticipantesSesionActualizados",
            It.Is<object?[]>(args => EsParticipantesActualizados(args, sesionId)),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task EquiposSesionActualizados_IncluyeEquipoOpcional()
    {
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarEquiposSesionActualizadosAsync(
            sesionId, equipoId, CancellationToken.None);

        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "EquiposSesionActualizados",
            It.Is<object?[]>(args => EsEquiposActualizados(args, sesionId, equipoId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EquipoActualizado_NotificaEquipoYSesion()
    {
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarEquipoActualizadoAsync(
            sesionId, equipoId, CancellationToken.None);

        arranque.Clients.Verify(c => c.Group(SesionesHub.GrupoEquipo(equipoId)), Times.Once);
        arranque.Clients.Verify(c => c.Group(SesionesHub.GrupoSesion(sesionId)), Times.Once);
        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "EquipoActualizado",
            It.Is<object?[]>(args => EsEquipoActualizado(args, sesionId, equipoId)),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task SesionActualizada_NotificaSesionYListado()
    {
        var sesionId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarSesionActualizadaAsync(
            sesionId, "Activa", CancellationToken.None);

        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "SesionActualizada",
            It.Is<object?[]>(args => EsSesionActualizada(args, sesionId, "Activa")),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ParticipanteExpulsado_NotificaUsuario()
    {
        var participanteIdentidadId = Guid.NewGuid();
        var participanteSesionId = Guid.NewGuid();
        var sesionId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarParticipanteExpulsadoAsync(
            participanteIdentidadId, sesionId, participanteSesionId, CancellationToken.None);

        arranque.Clients.Verify(c => c.User(participanteIdentidadId.ToString()), Times.Once);
        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "ParticipanteExpulsadoSesion",
            It.Is<object?[]>(args => EsParticipanteExpulsado(args, sesionId, participanteSesionId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EquipoExpulsado_SinParticipantes_NoNotifica()
    {
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarEquipoExpulsadoAsync(
            Array.Empty<Guid>(), Guid.NewGuid(), Guid.NewGuid(), "Rojo", CancellationToken.None);

        arranque.Proxy.Verify(c => c.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
        arranque.Clients.Verify(c => c.Users(It.IsAny<IReadOnlyList<string>>()), Times.Never);
    }

    [Fact]
    public async Task EquipoExpulsado_NotificaAIntegrantes()
    {
        var participantes = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarEquipoExpulsadoAsync(
            participantes, sesionId, equipoId, "Rojo", CancellationToken.None);

        arranque.Clients.Verify(c => c.Users(It.Is<IReadOnlyList<string>>(ids =>
            ids.SequenceEqual(participantes.Select(p => p.ToString())))), Times.Once);
        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "EquipoExpulsadoSesion",
            It.Is<object?[]>(args => EsEquipoExpulsado(args, sesionId, equipoId, "Rojo")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RespuestaRegistrada_IncluyeDatosDeLaRespuesta()
    {
        var sesionId = Guid.NewGuid();
        var etapaId = Guid.NewGuid();
        var preguntaId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarRespuestaRegistradaAsync(
            sesionId, etapaId, preguntaId, participanteId, equipoId, true, CancellationToken.None);

        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "RespuestaRegistrada",
            It.Is<object?[]>(args => EsRespuestaRegistrada(
                args, sesionId, etapaId, preguntaId, participanteId, equipoId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EtapaCompletada_IncluyeMisionYEtapa()
    {
        var sesionId = Guid.NewGuid();
        var misionId = Guid.NewGuid();
        var etapaId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarEtapaCompletadaAsync(
            sesionId, misionId, etapaId, CancellationToken.None);

        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "EtapaCompletada",
            It.Is<object?[]>(args => EsEtapaCompletada(args, sesionId, misionId, etapaId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EtapaIniciada_IncluyeDatosDeInicio()
    {
        var sesionId = Guid.NewGuid();
        var misionId = Guid.NewGuid();
        var etapaId = Guid.NewGuid();
        var modoId = Guid.NewGuid();
        var inicio = new DateTime(2026, 7, 16, 15, 0, 0, DateTimeKind.Utc);
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarEtapaIniciadaAsync(
            sesionId, misionId, etapaId, "Trivia", modoId, 3, inicio, 90, CancellationToken.None);

        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "EtapaIniciada",
            It.Is<object?[]>(args => EsEtapaIniciada(
                args, sesionId, misionId, etapaId, modoId, inicio)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EtapaPorComenzar_IncluyeDatosDePreparacion()
    {
        var sesionId = Guid.NewGuid();
        var misionId = Guid.NewGuid();
        var etapaId = Guid.NewGuid();
        var modoId = Guid.NewGuid();
        var inicio = new DateTime(2026, 7, 16, 15, 0, 0, DateTimeKind.Utc);
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarEtapaPorComenzarAsync(
            sesionId, misionId, etapaId, "BusquedaTesoro", modoId,
            2, 1, 5, true, inicio, 10, CancellationToken.None);

        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "EtapaPorComenzar",
            It.Is<object?[]>(args => EsEtapaPorComenzar(
                args, sesionId, misionId, etapaId, modoId, inicio)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PistaLiberada_IncluyeDatosDePista()
    {
        var sesionId = Guid.NewGuid();
        var etapaId = Guid.NewGuid();
        var pistaId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var notificador = new NotificadorSesionesTiempoReal(arranque.Hub.Object);

        await notificador.NotificarPistaLiberadaAsync(
            sesionId, etapaId, pistaId, "Busca la fuente", "Gps", 10.5, -66.9, CancellationToken.None);

        arranque.Proxy.Verify(c => c.SendCoreAsync(
            "PistaLiberada",
            It.Is<object?[]>(args => EsPistaLiberada(args, sesionId, etapaId, pistaId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProgresoSecuencialIndividual_notificaUsuarioYGrupoSesion()
    {
        var sesionId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var hub = new Mock<IHubContext<SesionesHub>>();
        var clients = new Mock<IHubClients>();
        var grupoSesion = new Mock<IClientProxy>();
        var usuario = new Mock<IClientProxy>();
        hub.SetupGet(h => h.Clients).Returns(clients.Object);
        clients.Setup(c => c.Group(SesionesHub.GrupoSesion(sesionId))).Returns(grupoSesion.Object);
        clients.Setup(c => c.User(participanteId.ToString())).Returns(usuario.Object);
        var notificador = new NotificadorSesionesTiempoReal(hub.Object);

        await notificador.NotificarProgresoSecuencialActualizadoAsync(
            sesionId, participanteId, equipoId: null, CancellationToken.None);

        VerificarProgresoEnviado(grupoSesion, sesionId, participanteId, equipoId: null);
        VerificarProgresoEnviado(usuario, sesionId, participanteId, equipoId: null);
    }

    [Fact]
    public async Task ProgresoSecuencialGrupal_notificaEquipoYGrupoSesion()
    {
        var sesionId = Guid.NewGuid();
        var participanteId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var hub = new Mock<IHubContext<SesionesHub>>();
        var clients = new Mock<IHubClients>();
        var grupoSesion = new Mock<IClientProxy>();
        var grupoEquipo = new Mock<IClientProxy>();
        hub.SetupGet(h => h.Clients).Returns(clients.Object);
        clients.Setup(c => c.Group(SesionesHub.GrupoSesion(sesionId))).Returns(grupoSesion.Object);
        clients.Setup(c => c.Group(SesionesHub.GrupoEquipo(equipoId))).Returns(grupoEquipo.Object);
        var notificador = new NotificadorSesionesTiempoReal(hub.Object);

        await notificador.NotificarProgresoSecuencialActualizadoAsync(
            sesionId, participanteId, equipoId, CancellationToken.None);

        VerificarProgresoEnviado(grupoSesion, sesionId, participanteId, equipoId);
        VerificarProgresoEnviado(grupoEquipo, sesionId, participanteId, equipoId);
    }

    private static void VerificarProgresoEnviado(
        Mock<IClientProxy> cliente,
        Guid sesionId,
        Guid participanteId,
        Guid? equipoId)
    {
        cliente.Verify(c => c.SendCoreAsync(
            "ProgresoSecuencialActualizado",
            It.Is<object?[]>(args => EsProgresoEsperado(args, sesionId, participanteId, equipoId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static bool EsProgresoEsperado(
        object?[] args,
        Guid sesionId,
        Guid participanteId,
        Guid? equipoId)
    {
        if (args.Length != 1) return false;
        var dto = args[0] as ProgresoSecuencialActualizadoDto;
        return dto is not null
               && dto.SesionId == sesionId
               && dto.ParticipanteIdentidadId == participanteId
               && dto.EquipoId == equipoId;
    }

    private static T? ObtenerDto<T>(object?[] args) where T : class
        => args.Length == 1 ? args[0] as T : null;

    private static bool EsParticipantesActualizados(object?[] args, Guid sesionId)
        => ObtenerDto<ParticipantesSesionActualizadosDto>(args)?.SesionId == sesionId;

    private static bool EsEquiposActualizados(object?[] args, Guid sesionId, Guid equipoId)
    {
        var dto = ObtenerDto<EquiposSesionActualizadosDto>(args);
        return dto is not null && dto.SesionId == sesionId && dto.EquipoId == equipoId;
    }

    private static bool EsEquipoActualizado(object?[] args, Guid sesionId, Guid equipoId)
    {
        var dto = ObtenerDto<EquipoActualizadoTiempoRealDto>(args);
        return dto is not null && dto.SesionId == sesionId && dto.EquipoId == equipoId;
    }

    private static bool EsSesionActualizada(object?[] args, Guid sesionId, string estado)
    {
        var dto = ObtenerDto<SesionActualizadaTiempoRealDto>(args);
        return dto is not null && dto.SesionId == sesionId && dto.Estado == estado;
    }

    private static bool EsParticipanteExpulsado(
        object?[] args,
        Guid sesionId,
        Guid participanteSesionId)
    {
        var dto = ObtenerDto<ParticipanteExpulsadoSesionDto>(args);
        return dto is not null
               && dto.SesionId == sesionId
               && dto.ParticipanteSesionId == participanteSesionId;
    }

    private static bool EsEquipoExpulsado(
        object?[] args,
        Guid sesionId,
        Guid equipoId,
        string equipoNombre)
    {
        var dto = ObtenerDto<EquipoExpulsadoSesionDto>(args);
        return dto is not null
               && dto.SesionId == sesionId
               && dto.EquipoId == equipoId
               && dto.EquipoNombre == equipoNombre;
    }

    private static bool EsRespuestaRegistrada(
        object?[] args,
        Guid sesionId,
        Guid etapaId,
        Guid preguntaId,
        Guid participanteId,
        Guid equipoId)
    {
        var dto = ObtenerDto<RespuestaRegistradaDto>(args);
        return dto is not null
               && dto.SesionId == sesionId
               && dto.EtapaId == etapaId
               && dto.PreguntaId == preguntaId
               && dto.ParticipanteIdentidadId == participanteId
               && dto.EquipoId == equipoId
               && dto.EsCorrecta;
    }

    private static bool EsEtapaCompletada(
        object?[] args,
        Guid sesionId,
        Guid misionId,
        Guid etapaId)
    {
        var dto = ObtenerDto<EtapaCompletadaDto>(args);
        return dto is not null
               && dto.SesionId == sesionId
               && dto.MisionId == misionId
               && dto.EtapaId == etapaId;
    }

    private static bool EsEtapaIniciada(
        object?[] args,
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        Guid modoId,
        DateTime inicio)
    {
        var dto = ObtenerDto<EtapaIniciadaDto>(args);
        return dto is not null
               && dto.SesionId == sesionId
               && dto.MisionId == misionId
               && dto.EtapaId == etapaId
               && dto.TipoEtapa == "Trivia"
               && dto.ModoDeJuegoId == modoId
               && dto.OrdenGlobal == 3
               && dto.FechaInicioEtapaUtc == inicio
               && dto.DuracionSegundos == 90;
    }

    private static bool EsEtapaPorComenzar(
        object?[] args,
        Guid sesionId,
        Guid misionId,
        Guid etapaId,
        Guid modoId,
        DateTime inicio)
    {
        var dto = ObtenerDto<EtapaPorComenzarDto>(args);
        return dto is not null
               && dto.SesionId == sesionId
               && dto.MisionId == misionId
               && dto.EtapaId == etapaId
               && dto.TipoEtapa == "BusquedaTesoro"
               && dto.ModoDeJuegoId == modoId
               && dto.NumeroMision == 2
               && dto.NumeroEtapa == 1
               && dto.OrdenGlobal == 5
               && dto.EsNuevaMision
               && dto.FechaInicioProgramadaUtc == inicio
               && dto.DuracionPreparacionSegundos == 10;
    }

    private static bool EsPistaLiberada(
        object?[] args,
        Guid sesionId,
        Guid etapaId,
        Guid pistaId)
    {
        var dto = ObtenerDto<PistaLiberadaDto>(args);
        return dto is not null
               && dto.SesionId == sesionId
               && dto.EtapaId == etapaId
               && dto.PistaId == pistaId
               && dto.Contenido == "Busca la fuente"
               && dto.Tipo == "Gps"
               && dto.Latitud == 10.5
               && dto.Longitud == -66.9;
    }

    private sealed class ArranqueHub
    {
        public Mock<IHubContext<SesionesHub>> Hub { get; } = new();
        public Mock<IHubClients> Clients { get; } = new();
        public Mock<IClientProxy> Proxy { get; } = new();

        public ArranqueHub()
        {
            Hub.SetupGet(h => h.Clients).Returns(Clients.Object);
            Clients.Setup(c => c.Group(It.IsAny<string>())).Returns(Proxy.Object);
            Clients.Setup(c => c.User(It.IsAny<string>())).Returns(Proxy.Object);
            Clients.Setup(c => c.Users(It.IsAny<IReadOnlyList<string>>())).Returns(Proxy.Object);
        }
    }
}
