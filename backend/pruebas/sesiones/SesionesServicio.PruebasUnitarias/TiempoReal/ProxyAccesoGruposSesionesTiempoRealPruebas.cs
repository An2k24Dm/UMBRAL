using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Infraestructura.TiempoReal.Grupos;
using SesionesServicio.Infraestructura.TiempoReal.Hubs;
using SesionesServicio.PruebasUnitarias.Dominio; // EquipoTestHelpers (CrearEquipo de 4 args)

namespace SesionesServicio.PruebasUnitarias.TiempoReal;

// Pruebas del patrón Proxy de control de acceso a grupos SignalR. Verifican
// tanto las reglas de acceso como la delegación: el sujeto real se invoca
// exactamente una vez cuando se autoriza y nunca cuando se deniega. La interfaz
// (puerto) usa identidad primitiva (usuarioId + roles); los actores se arman
// con ContextoActorTiempoReal solo por comodidad de las pruebas.
public class ProxyAccesoGruposSesionesTiempoRealPruebas
{
    private static readonly DateTime AhoraUtc = new(2026, 7, 9, 10, 0, 0, DateTimeKind.Utc);
    private static readonly Guid Operador = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtroOperador = Guid.Parse("1a1a1a1a-1a1a-1a1a-1a1a-1a1a1a1a1a1a");
    private static readonly Guid ParticipanteInscrito = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ParticipanteAjeno = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Ana = Guid.Parse("a1a1a1a1-a1a1-a1a1-a1a1-a1a1a1a1a1a1");   // Rojo
    private static readonly Guid Pedro = Guid.Parse("b2b2b2b2-b2b2-b2b2-b2b2-b2b2b2b2b2b2"); // Rojo
    private static readonly Guid Carlos = Guid.Parse("c3c3c3c3-c3c3-c3c3-c3c3-c3c3c3c3c3c3"); // Azul
    private const string Conexion = "conn-1";

    // ----------------------------------------------------------------------
    // Sesiones de dominio
    // ----------------------------------------------------------------------

    private static SesionIndividual IndividualDe(Guid operadorId, Guid participante)
    {
        var s = SesionIndividual.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "COD001", operadorId, AhoraUtc, 5);
        s.Preparar();
        s.AgregarParticipante(participante, AhoraUtc);
        s.Iniciar(AhoraUtc);
        return s;
    }

    private static (SesionGrupal sesion, Guid rojoId, Guid azulId) GrupalDe(Guid operadorId)
    {
        var s = SesionGrupal.Crear(
            "Sesión", "Demo", AhoraUtc.AddHours(1), "COD002", operadorId, AhoraUtc,
            maximoEquipos: 5, maximoParticipantesPorEquipo: 2);
        s.Preparar();
        var rojo = s.CrearEquipo("Rojo", Ana, AhoraUtc, AhoraUtc);
        s.AgregarParticipanteAEquipo(rojo.Id, Pedro, AhoraUtc, AhoraUtc);
        var azul = s.CrearEquipo("Azul", Carlos, AhoraUtc, AhoraUtc);
        s.Iniciar(AhoraUtc);
        return (s, rojo.Id, azul.Id);
    }

    // ----------------------------------------------------------------------
    // Actores (comodidad) y llamadas al proxy con identidad primitiva
    // ----------------------------------------------------------------------

    private static ContextoActorTiempoReal Actor(Guid? uid, params string[] roles)
        => new(uid, roles, "usuario");

    private static ContextoActorTiempoReal Administrador(Guid? uid = null)
        => Actor(uid ?? Guid.NewGuid(), "Administrador");
    private static ContextoActorTiempoReal RolOperador(Guid uid) => Actor(uid, "Operador");
    private static ContextoActorTiempoReal Participante(Guid uid) => Actor(uid, "Participante");

    private static Task Listado(IServicioGruposSesionesTiempoReal proxy, ContextoActorTiempoReal a)
        => proxy.UnirseAListadoAsync(Conexion, a.UsuarioId, a.Roles, CancellationToken.None);
    private static Task Sesion(IServicioGruposSesionesTiempoReal proxy, ContextoActorTiempoReal a, Guid sesionId)
        => proxy.UnirseASesionAsync(Conexion, a.UsuarioId, a.Roles, sesionId, CancellationToken.None);
    private static Task Equipo(IServicioGruposSesionesTiempoReal proxy, ContextoActorTiempoReal a, Guid equipoId)
        => proxy.UnirseAEquipoAsync(Conexion, a.UsuarioId, a.Roles, equipoId, CancellationToken.None);

    private sealed class Arranque
    {
        public Mock<IServicioGruposSesionesTiempoReal> Real { get; } = new();
        public Mock<IRepositorioSesiones> Repo { get; } = new();

        public Arranque() => Real.SetReturnsDefault(Task.CompletedTask);

        public ProxyAccesoGruposSesionesTiempoReal Proxy() => new(
            Real.Object, Repo.Object,
            NullLogger<ProxyAccesoGruposSesionesTiempoReal>.Instance);
    }

    [Fact]
    public void Proxy_SujetoReal_YHub_UsanPuertoDeAplicacion()
    {
        typeof(IServicioGruposSesionesTiempoReal).IsAssignableFrom(
            typeof(ProxyAccesoGruposSesionesTiempoReal)).Should().BeTrue();
        typeof(IServicioGruposSesionesTiempoReal).IsAssignableFrom(
            typeof(ServicioGruposSesionesTiempoReal)).Should().BeTrue();

        var constructorHub = typeof(SesionesHub).GetConstructors().Should().ContainSingle().Subject;
        constructorHub.GetParameters().Should()
            .Contain(p => p.ParameterType == typeof(IServicioGruposSesionesTiempoReal));
    }

    // ======================================================================
    // LISTADO
    // ======================================================================

    [Fact] // (1)
    public async Task Listado_SinRolReconocido_EsDenegado()
    {
        var arr = new Arranque();
        Func<Task> accion = () => Listado(arr.Proxy(), Actor(Guid.NewGuid()));

        await accion.Should().ThrowAsync<HubException>();
        arr.Real.Verify(r => r.UnirseAListadoAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory] // (2, 3, 4)
    [InlineData("Administrador")]
    [InlineData("Operador")]
    [InlineData("Participante")]
    public async Task Listado_ConRolReconocido_EsPermitido(string rol)
    {
        var arr = new Arranque();
        await Listado(arr.Proxy(), Actor(Guid.NewGuid(), rol));

        arr.Real.Verify(r => r.UnirseAListadoAsync(
            Conexion, It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // ======================================================================
    // SESIÓN INDIVIDUAL
    // ======================================================================

    [Fact] // (5, 17) Participante inscrito puede unirse; delega una vez.
    public async Task Sesion_ParticipanteInscrito_EsPermitido()
    {
        var sesion = IndividualDe(Operador, ParticipanteInscrito);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        await Sesion(arr.Proxy(), Participante(ParticipanteInscrito), sesion.Id);

        arr.Real.Verify(r => r.UnirseASesionAsync(
            Conexion, It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (6, 18) Participante no inscrito es rechazado; el sujeto real nunca se llama.
    public async Task Sesion_ParticipanteNoInscrito_EsDenegado()
    {
        var sesion = IndividualDe(Operador, ParticipanteInscrito);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        Func<Task> accion = () => Sesion(arr.Proxy(), Participante(ParticipanteAjeno), sesion.Id);

        await accion.Should().ThrowAsync<HubException>();
        arr.Real.Verify(r => r.UnirseASesionAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ParticipanteNoInscrito_PuedeListadoPeroNoGrupoSesion()
    {
        var sesion = IndividualDe(Operador, ParticipanteInscrito);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);
        var proxy = arr.Proxy();
        var actor = Participante(ParticipanteAjeno);

        await Listado(proxy, actor);
        Func<Task> entrarASesion = () => Sesion(proxy, actor, sesion.Id);

        await entrarASesion.Should().ThrowAsync<HubException>();
        arr.Real.Verify(r => r.UnirseAListadoAsync(
            Conexion, actor.UsuarioId, actor.Roles, It.IsAny<CancellationToken>()), Times.Once);
        arr.Real.Verify(r => r.UnirseASesionAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (7)
    public async Task Sesion_OperadorDueno_EsPermitido()
    {
        var sesion = IndividualDe(Operador, ParticipanteInscrito);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        await Sesion(arr.Proxy(), RolOperador(Operador), sesion.Id);

        arr.Real.Verify(r => r.UnirseASesionAsync(
            Conexion, It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (8)
    public async Task Sesion_OperadorNoDueno_EsDenegado()
    {
        var sesion = IndividualDe(Operador, ParticipanteInscrito);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        Func<Task> accion = () => Sesion(arr.Proxy(), RolOperador(OtroOperador), sesion.Id);

        await accion.Should().ThrowAsync<HubException>();
        arr.Real.Verify(r => r.UnirseASesionAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (9)
    public async Task Sesion_Administrador_EsPermitido()
    {
        var sesion = IndividualDe(Operador, ParticipanteInscrito);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        await Sesion(arr.Proxy(), Administrador(), sesion.Id);

        arr.Real.Verify(r => r.UnirseASesionAsync(
            Conexion, It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ======================================================================
    // SESIÓN GRUPAL (grupo de sesión)
    // ======================================================================

    [Fact] // (10) Integrante de cualquier equipo de la sesión puede unirse al grupo de sesión.
    public async Task SesionGrupal_IntegranteDeEquipo_EsPermitido()
    {
        var (sesion, _, _) = GrupalDe(Operador);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        await Sesion(arr.Proxy(), Participante(Pedro), sesion.Id);

        arr.Real.Verify(r => r.UnirseASesionAsync(
            Conexion, It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            sesion.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (11) Participante que no pertenece a ningún equipo de la sesión es rechazado.
    public async Task SesionGrupal_ParticipanteSinEquipo_EsDenegado()
    {
        var (sesion, _, _) = GrupalDe(Operador);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorIdAsync(sesion.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        Func<Task> accion = () => Sesion(arr.Proxy(), Participante(ParticipanteAjeno), sesion.Id);

        await accion.Should().ThrowAsync<HubException>();
        arr.Real.Verify(r => r.UnirseASesionAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ======================================================================
    // EQUIPO (grupo de equipo)
    // ======================================================================

    [Fact] // (12) Integrante del equipo puede unirse al grupo del equipo.
    public async Task Equipo_Integrante_EsPermitido()
    {
        var (sesion, rojoId, _) = GrupalDe(Operador);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorEquipoIdAsync(rojoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        await Equipo(arr.Proxy(), Participante(Pedro), rojoId);

        arr.Real.Verify(r => r.UnirseAEquipoAsync(
            Conexion, It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            rojoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (13) Participante de otro equipo de la misma sesión es rechazado.
    public async Task Equipo_ParticipanteDeOtroEquipo_EsDenegado()
    {
        var (sesion, _, azulId) = GrupalDe(Operador);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorEquipoIdAsync(azulId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        // Pedro (equipo Rojo) intenta unirse al grupo del equipo Azul.
        Func<Task> accion = () => Equipo(arr.Proxy(), Participante(Pedro), azulId);

        await accion.Should().ThrowAsync<HubException>();
        arr.Real.Verify(r => r.UnirseAEquipoAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (14) Participante de otra sesión (ajeno a todos los equipos) es rechazado.
    public async Task Equipo_ParticipanteDeOtraSesion_EsDenegado()
    {
        var (sesion, rojoId, _) = GrupalDe(Operador);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorEquipoIdAsync(rojoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        Func<Task> accion = () => Equipo(arr.Proxy(), Participante(ParticipanteAjeno), rojoId);

        await accion.Should().ThrowAsync<HubException>();
        arr.Real.Verify(r => r.UnirseAEquipoAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact] // (15)
    public async Task Equipo_OperadorDueno_EsPermitido()
    {
        var (sesion, rojoId, _) = GrupalDe(Operador);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorEquipoIdAsync(rojoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        await Equipo(arr.Proxy(), RolOperador(Operador), rojoId);

        arr.Real.Verify(r => r.UnirseAEquipoAsync(
            Conexion, It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            rojoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // (16)
    public async Task Equipo_Administrador_EsPermitido()
    {
        var (sesion, rojoId, _) = GrupalDe(Operador);
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorEquipoIdAsync(rojoId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sesion);

        await Equipo(arr.Proxy(), Administrador(), rojoId);

        arr.Real.Verify(r => r.UnirseAEquipoAsync(
            Conexion, It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            rojoId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact] // Equipo inexistente (sin sesión propietaria) es rechazado.
    public async Task Equipo_Inexistente_EsDenegado()
    {
        var arr = new Arranque();
        arr.Repo.Setup(r => r.ObtenerPorEquipoIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sesion?)null);

        Func<Task> accion = () => Equipo(arr.Proxy(), Participante(Pedro), Guid.NewGuid());

        await accion.Should().ThrowAsync<HubException>();
        arr.Real.Verify(r => r.UnirseAEquipoAsync(
            It.IsAny<string>(), It.IsAny<Guid?>(), It.IsAny<IReadOnlyCollection<string>>(),
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ======================================================================
    // SALIR: no requiere autorización, siempre delega.
    // ======================================================================

    [Fact]
    public async Task SalirDeSesion_SiempreDelega()
    {
        var arr = new Arranque();
        var id = Guid.NewGuid();

        await arr.Proxy().SalirDeSesionAsync(Conexion, id, CancellationToken.None);

        arr.Real.Verify(r => r.SalirDeSesionAsync(Conexion, id, It.IsAny<CancellationToken>()),
            Times.Once);
        arr.Repo.Verify(r => r.ObtenerPorIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
