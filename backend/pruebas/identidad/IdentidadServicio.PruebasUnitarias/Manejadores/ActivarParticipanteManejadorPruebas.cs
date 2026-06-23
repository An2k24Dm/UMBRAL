using IdentidadServicio.Aplicacion.Comandos.ActivarParticipante;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

// Pruebas del manejador de reactivación de Participante. Simétricas a las
// de DesactivarParticipante: validan que solo Administrador u Operador
// Activo puedan invocar, que la cuenta esté Inactiva para poder activarse,
// y que no se toquen datos personales ni Keycloak.
public class ActivarParticipanteManejadorPruebas
{
    private readonly Mock<IAutorizadorUsuarioActivo> _autorizador = new();
    private readonly Mock<IRepositorioParticipantes> _repositorio = new();
    private readonly Mock<IUnidadTrabajoIdentidad> _unidad = new();

    public ActivarParticipanteManejadorPruebas()
    {
        // Por defecto un Administrador Activo invoca.
        _autorizador
            .Setup(a => a.RequerirRolesActivosAsync(
                It.IsAny<IEnumerable<RolUsuario>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UsuariosDePrueba.NuevoAdministrador());
        _repositorio
            .Setup(r => r.ActualizarEstadoAsync(It.IsAny<Participante>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unidad
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private ActivarParticipanteManejador CrearManejador()
        => new(
            _autorizador.Object,
            _repositorio.Object,
            _unidad.Object,
            NullLogger<ActivarParticipanteManejador>.Instance);

    private static Participante ParticipanteInactivo()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        p.Desactivar();
        return p;
    }

    [Fact]
    public async Task ParticipanteInvocador_LanzaAccesoNoPermitido()
    {
        _autorizador
            .Setup(a => a.RequerirRolesActivosAsync(
                It.IsAny<IEnumerable<RolUsuario>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesoNoPermitidoExcepcion("Su rol no le permite realizar esta acción."));

        Func<Task> accion = () => CrearManejador().Handle(
            new ActivarParticipanteComando(Guid.NewGuid()), CancellationToken.None);
        await accion.Should().ThrowAsync<AccesoNoPermitidoExcepcion>();

        _repositorio.Verify(r => r.ActualizarEstadoAsync(
            It.IsAny<Participante>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OperadorInactivoInvocador_LanzaCuentaDesactivada()
    {
        _autorizador
            .Setup(a => a.RequerirRolesActivosAsync(
                It.IsAny<IEnumerable<RolUsuario>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CuentaDesactivadaExcepcion());

        Func<Task> accion = () => CrearManejador().Handle(
            new ActivarParticipanteComando(Guid.NewGuid()), CancellationToken.None);
        await accion.Should().ThrowAsync<CuentaDesactivadaExcepcion>();
    }

    [Fact]
    public async Task OperadorActivoInvocador_ActivaParticipante()
    {
        _autorizador
            .Setup(a => a.RequerirRolesActivosAsync(
                It.IsAny<IEnumerable<RolUsuario>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UsuariosDePrueba.NuevoOperador());

        var p = ParticipanteInactivo();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(p);

        var resultado = await CrearManejador().Handle(
            new ActivarParticipanteComando(p.Id), CancellationToken.None);

        p.Estado.Should().Be(EstadoUsuario.Activo);
        resultado.Estado.Should().Be("Activo");
        _repositorio.Verify(r => r.ActualizarEstadoAsync(p, It.IsAny<CancellationToken>()),
            Times.Once);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ParticipanteInexistente_Retorna404Controlado()
    {
        var id = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Participante?)null);

        Func<Task> accion = () => CrearManejador().Handle(
            new ActivarParticipanteComando(id), CancellationToken.None);
        (await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>())
            .Which.Message.Should().Contain("No existe un Participante");
    }

    [Fact]
    public async Task ParticipanteYaActivo_LanzaUsuarioYaActivo()
    {
        var p = UsuariosDePrueba.NuevoParticipante(); // Activo por defecto
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(p);

        Func<Task> accion = () => CrearManejador().Handle(
            new ActivarParticipanteComando(p.Id), CancellationToken.None);
        await accion.Should().ThrowAsync<UsuarioYaActivoExcepcion>();

        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdministradorInvocador_ActivaParticipanteYNoTocaDatos()
    {
        var p = UsuariosDePrueba.NuevoParticipante(
            nombreUsuario: "pablito01", correo: "pablo@umbral.com",
            nombre: "Pablo", apellido: "Participante", alias: "pablito");
        p.Desactivar();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(p);

        await CrearManejador().Handle(
            new ActivarParticipanteComando(p.Id), CancellationToken.None);

        p.Estado.Should().Be(EstadoUsuario.Activo);
        p.Rol.Should().Be(RolUsuario.Participante);
        p.NombreUsuario.Valor.Should().Be("pablito01");
        p.Correo.Valor.Should().Be("pablo@umbral.com");
        p.Alias.Should().Be("pablito");
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
