using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Infraestructura.TiempoReal;
using Xunit;

namespace RankingServicio.PruebasUnitarias.Infraestructura;

public class RankingTiempoRealPruebas
{
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string ConexionId = "ranking-conn";

    [Fact]
    public async Task Hub_UnirseYSalirDeSesion_AdministraGrupo()
    {
        var sesionId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(UsuarioConNameIdentifier());

        await hub.UnirseASesion(sesionId.ToString());
        await hub.SalirDeSesion(sesionId.ToString());

        arranque.Grupos.Verify(g => g.AddToGroupAsync(
            ConexionId, $"sesion:{sesionId}", It.IsAny<CancellationToken>()), Times.Once);
        arranque.Grupos.Verify(g => g.RemoveFromGroupAsync(
            ConexionId, $"sesion:{sesionId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(typeof(OperationCanceledException))]
    [InlineData(typeof(InvalidOperationException))]
    public async Task Hub_OnDisconnected_RegistraSegunTipo(Type? tipoExcepcion)
    {
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(UsuarioConSub());
        var excepcion = tipoExcepcion is null
            ? null
            : (Exception)Activator.CreateInstance(tipoExcepcion, "boom")!;

        await hub.OnDisconnectedAsync(excepcion);

        arranque.Logger.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Hub_OnConnected_UsaUserIdentifierSiExiste()
    {
        var arranque = new ArranqueHub(userIdentifier: "user-provider-id");
        var hub = arranque.CrearHub(UsuarioConNameIdentifier());

        await hub.OnConnectedAsync();

        arranque.Logger.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Hub_OnConnected_SinClaimsUsaDesconocido()
    {
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(new ClaimsPrincipal(new ClaimsIdentity()));

        await hub.OnConnectedAsync();

        arranque.Logger.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Notificador_EnviaPuntajeCalculadoAlGrupoSesion()
    {
        var sesionId = Guid.NewGuid();
        var puntaje = new PuntajeCalculadoDto(
            Guid.NewGuid(), sesionId, Guid.NewGuid(), Guid.NewGuid(), null, 10, 50, null,
            DateTime.UtcNow);
        var arranque = new ArranqueNotificador();
        var notificador = new NotificadorRankingTiempoReal(arranque.Hub.Object);

        await notificador.NotificarPuntajeCalculadoAsync(puntaje, CancellationToken.None);

        arranque.Clients.Verify(c => c.Group($"sesion:{sesionId}"), Times.Once);
        arranque.Cliente.Verify(c => c.SendCoreAsync(
            "PuntajeCalculado",
            It.Is<object?[]>(args => args.Length == 1 && ReferenceEquals(args[0], puntaje)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("participantes", "RankingParticipantesActualizado")]
    [InlineData("equipos", "RankingEquiposActualizado")]
    public async Task Notificador_EnviaActualizacionDeRanking(string tipo, string evento)
    {
        var sesionId = Guid.NewGuid();
        var arranque = new ArranqueNotificador();
        var notificador = new NotificadorRankingTiempoReal(arranque.Hub.Object);

        if (tipo == "participantes")
            await notificador.NotificarRankingParticipantesActualizadoAsync(sesionId, CancellationToken.None);
        else
            await notificador.NotificarRankingEquiposActualizadoAsync(sesionId, CancellationToken.None);

        arranque.Cliente.Verify(c => c.SendCoreAsync(
            evento,
            It.Is<object?[]>(args => args.Length == 1 && (Guid)args[0]! == sesionId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ClaimsPrincipal UsuarioConNameIdentifier()
        => new(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, UsuarioId.ToString()) },
            "Pruebas"));

    private static ClaimsPrincipal UsuarioConSub()
        => new(new ClaimsIdentity(
            new[] { new Claim("sub", UsuarioId.ToString()) },
            "Pruebas"));

    private sealed class ArranqueHub
    {
        public Mock<IGroupManager> Grupos { get; } = new();
        public Mock<ILogger<RankingHub>> Logger { get; } = new();
        private readonly string? _userIdentifier;

        public ArranqueHub(string? userIdentifier = null)
        {
            _userIdentifier = userIdentifier;
        }

        public RankingHub CrearHub(ClaimsPrincipal usuario)
        {
            var contexto = new Mock<HubCallerContext>();
            contexto.SetupGet(c => c.ConnectionId).Returns(ConexionId);
            contexto.SetupGet(c => c.UserIdentifier).Returns(_userIdentifier);
            contexto.SetupGet(c => c.User).Returns(usuario);
            contexto.SetupGet(c => c.ConnectionAborted).Returns(CancellationToken.None);

            return new RankingHub(Logger.Object)
            {
                Context = contexto.Object,
                Groups = Grupos.Object
            };
        }
    }

    private sealed class ArranqueNotificador
    {
        public Mock<IHubContext<RankingHub>> Hub { get; } = new();
        public Mock<IHubClients> Clients { get; } = new();
        public Mock<IClientProxy> Cliente { get; } = new();

        public ArranqueNotificador()
        {
            Hub.SetupGet(h => h.Clients).Returns(Clients.Object);
            Clients.Setup(c => c.Group(It.IsAny<string>())).Returns(Cliente.Object);
        }
    }
}
