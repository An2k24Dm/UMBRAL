using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos.TiempoReal;
using SesionesServicio.Infraestructura.TiempoReal;
using SesionesServicio.Infraestructura.TiempoReal.Hubs;

namespace SesionesServicio.PruebasUnitarias.TiempoReal;

public class SesionesHubPruebas
{
    private static readonly Guid UsuarioId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private const string ConexionId = "conn-1";

    [Fact]
    public async Task UnirseAListadoSesiones_PropagaActorYRoles()
    {
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(Usuario(UsuarioId, "ana", "Operador", "Participante"));

        await hub.UnirseAListadoSesiones();

        arranque.Grupos.Verify(g => g.UnirseAListadoAsync(
            ConexionId,
            UsuarioId,
            It.Is<IReadOnlyCollection<string>>(roles =>
                roles.SequenceEqual(new[] { "Operador", "Participante" })),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnirseASesion_ConGuidValido_PropagaSesion()
    {
        var sesionId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(Usuario(UsuarioId, "ana", "Operador"));

        await hub.UnirseASesion(sesionId.ToString());

        arranque.Grupos.Verify(g => g.UnirseASesionAsync(
            ConexionId,
            UsuarioId,
            It.Is<IReadOnlyCollection<string>>(roles => roles.Contains("Operador")),
            sesionId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UnirseAEquipo_ConGuidValido_PropagaEquipo()
    {
        var equipoId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(Usuario(UsuarioId, "ana", "Participante"));

        await hub.UnirseAEquipo(equipoId.ToString());

        arranque.Grupos.Verify(g => g.UnirseAEquipoAsync(
            ConexionId,
            UsuarioId,
            It.Is<IReadOnlyCollection<string>>(roles => roles.Contains("Participante")),
            equipoId,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("sesion")]
    [InlineData("equipo")]
    public async Task UnirseAGrupo_ConGuidInvalido_LanzaHubException(string recurso)
    {
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(Usuario(UsuarioId, "ana", "Operador"));

        Func<Task> accion = recurso == "sesion"
            ? () => hub.UnirseASesion("no-guid")
            : () => hub.UnirseAEquipo("no-guid");

        var recursoEsperado = recurso == "sesion" ? "sesión" : recurso;

        await accion.Should().ThrowAsync<HubException>()
            .WithMessage($"*{recursoEsperado}*no es válido*");
    }

    [Fact]
    public async Task SalirDeSesion_ConGuidInvalido_NoInvocaGrupos()
    {
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(Usuario(UsuarioId, "ana", "Operador"));

        await hub.SalirDeSesion("no-guid");

        arranque.Grupos.Verify(g => g.SalirDeSesionAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SalirDeEquipo_ConGuidValido_InvocaServicio()
    {
        var equipoId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(Usuario(UsuarioId, "ana", "Participante"));

        await hub.SalirDeEquipo(equipoId.ToString());

        arranque.Grupos.Verify(g => g.SalirDeEquipoAsync(
            ConexionId, equipoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SalirDeListadoSesiones_InvocaServicio()
    {
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(Usuario(UsuarioId, "ana", "Operador"));

        await hub.SalirDeListadoSesiones();

        arranque.Grupos.Verify(g => g.SalirDeListadoAsync(
            ConexionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(typeof(OperationCanceledException))]
    [InlineData(typeof(InvalidOperationException))]
    public async Task OnDisconnectedAsync_RegistraDesconexionSegunTipo(Type? tipoExcepcion)
    {
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(Usuario(UsuarioId, "ana", "Operador"));
        var excepcion = tipoExcepcion is null
            ? null
            : (Exception)Activator.CreateInstance(tipoExcepcion, "boom")!;

        await hub.OnDisconnectedAsync(excepcion);

        arranque.Logger.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnConnectedAsync_RegistraActor()
    {
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(Usuario(UsuarioId, "ana", "Operador"));

        await hub.OnConnectedAsync();

        arranque.Logger.Invocations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task EnviarUbicacion_SinUsuarioIdentificado_NoActualizaNiNotifica()
    {
        var sesionId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(new ClaimsPrincipal(new ClaimsIdentity()));

        await hub.EnviarUbicacion(sesionId.ToString(), null, 10.5, -66.9);

        arranque.Almacen.Verify(a => a.Actualizar(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>(),
            It.IsAny<double>(), It.IsAny<double>()), Times.Never);
        arranque.Cliente.Verify(c => c.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnviarUbicacion_ConEquipo_ActualizaYNotificaOperadoresYEquipo()
    {
        var sesionId = Guid.NewGuid();
        var equipoId = Guid.NewGuid();
        var arranque = new ArranqueHub();
        var hub = arranque.CrearHub(Usuario(UsuarioId, "ana", "Participante"));

        await hub.EnviarUbicacion(sesionId.ToString(), equipoId.ToString(), 10.5, -66.9);

        arranque.Almacen.Verify(a => a.Actualizar(
            sesionId, UsuarioId, "ana", equipoId, 10.5, -66.9), Times.Once);
        arranque.Clients.Verify(c => c.Group(SesionesHub.GrupoOperadoresSesion(sesionId)), Times.Once);
        arranque.Clients.Verify(c => c.Group(SesionesHub.GrupoEquipo(equipoId)), Times.Once);
        arranque.Cliente.Verify(c => c.SendCoreAsync(
            "UbicacionActualizada",
            It.Is<object?[]>(args => EsUbicacion(args, sesionId, equipoId)),
            It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static ClaimsPrincipal Usuario(Guid usuarioId, string nombre, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new("preferred_username", nombre)
        };
        claims.AddRange(roles.Select(rol => new Claim("roles", rol)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Pruebas"));
    }

    private static bool EsUbicacion(object?[] args, Guid sesionId, Guid equipoId)
    {
        if (args.Length != 1 || args[0] is not UbicacionActualizadaDto dto)
            return false;

        return dto.SesionId == sesionId
               && dto.ParticipanteIdentidadId == UsuarioId
               && dto.Nombre == "ana"
               && dto.EquipoId == equipoId
               && dto.Latitud == 10.5
               && dto.Longitud == -66.9;
    }

    private sealed class ArranqueHub
    {
        public Mock<IServicioGruposSesionesTiempoReal> Grupos { get; } = new();
        public Mock<IAlmacenUbicaciones> Almacen { get; } = new();
        public Mock<ILogger<SesionesHub>> Logger { get; } = new();
        public Mock<IHubCallerClients> Clients { get; } = new();
        public Mock<IClientProxy> Cliente { get; } = new();

        public SesionesHub CrearHub(ClaimsPrincipal usuario)
        {
            var contexto = new Mock<HubCallerContext>();
            contexto.SetupGet(c => c.ConnectionId).Returns(ConexionId);
            contexto.SetupGet(c => c.UserIdentifier).Returns((string?)null);
            contexto.SetupGet(c => c.User).Returns(usuario);
            contexto.SetupGet(c => c.ConnectionAborted).Returns(CancellationToken.None);
            Clients.Setup(c => c.Group(It.IsAny<string>())).Returns(Cliente.Object);

            return new SesionesHub(Grupos.Object, Almacen.Object, Logger.Object)
            {
                Context = contexto.Object,
                Clients = Clients.Object
            };
        }
    }
}
