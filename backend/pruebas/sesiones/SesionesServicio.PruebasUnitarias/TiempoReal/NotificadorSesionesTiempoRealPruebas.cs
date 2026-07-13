using Microsoft.AspNetCore.SignalR;
using SesionesServicio.Commons.Dtos.TiempoReal;
using SesionesServicio.Infraestructura.TiempoReal;
using SesionesServicio.Infraestructura.TiempoReal.Hubs;

namespace SesionesServicio.PruebasUnitarias.TiempoReal;

public sealed class NotificadorSesionesTiempoRealPruebas
{
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
}
