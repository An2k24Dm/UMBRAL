using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

// HU12 (extensión) — pruebas del manejador de desactivación de Participante.
// Cubren:
//  * Administrador Activo o Operador Activo pueden invocar.
//  * Operador Inactivo es rechazado por el autorizador.
//  * Participante no puede invocar (rechazo por autorizador).
//  * 404 si el Participante no existe.
//  * 400 USUARIO_YA_INACTIVO si ya está Inactivo.
//  * Cambia Estado a Inactivo y guarda.
//  * No borra datos ni llama Keycloak DELETE.
public class DesactivarParticipanteManejadorPruebas
{
    private readonly Mock<IAutorizadorUsuarioActivo> _autorizador = new();
    private readonly Mock<IRepositorioParticipantes> _repositorio = new();
    private readonly Mock<IUnidadTrabajoIdentidad> _unidad = new();

    public DesactivarParticipanteManejadorPruebas()
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

    private DesactivarParticipanteManejador CrearManejador()
        => new(
            _autorizador.Object,
            _repositorio.Object,
            _unidad.Object,
            NullLogger<DesactivarParticipanteManejador>.Instance);

    [Fact]
    public async Task ParticipanteInvocador_LanzaAccesoNoPermitido()
    {
        // El autorizador rechaza roles no permitidos.
        _autorizador
            .Setup(a => a.RequerirRolesActivosAsync(
                It.IsAny<IEnumerable<RolUsuario>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesoNoPermitidoExcepcion("Su rol no le permite realizar esta acción."));

        Func<Task> accion = () => CrearManejador().Handle(
            new DesactivarParticipanteComando(Guid.NewGuid()), CancellationToken.None);
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
            new DesactivarParticipanteComando(Guid.NewGuid()), CancellationToken.None);
        await accion.Should().ThrowAsync<CuentaDesactivadaExcepcion>();
    }

    [Fact]
    public async Task OperadorActivoInvocador_DesactivaParticipante()
    {
        _autorizador
            .Setup(a => a.RequerirRolesActivosAsync(
                It.IsAny<IEnumerable<RolUsuario>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UsuariosDePrueba.NuevoOperador());

        var p = UsuariosDePrueba.NuevoParticipante();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(p);

        var resultado = await CrearManejador().Handle(
            new DesactivarParticipanteComando(p.Id), CancellationToken.None);

        p.Estado.Should().Be(EstadoUsuario.Inactivo);
        resultado.Estado.Should().Be("Inactivo");
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
            new DesactivarParticipanteComando(id), CancellationToken.None);
        (await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>())
            .Which.Message.Should().Contain("No existe un Participante");
    }

    [Fact]
    public async Task ParticipanteYaInactivo_LanzaUsuarioYaInactivo()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        p.Desactivar();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(p);

        Func<Task> accion = () => CrearManejador().Handle(
            new DesactivarParticipanteComando(p.Id), CancellationToken.None);
        await accion.Should().ThrowAsync<UsuarioYaInactivoExcepcion>();

        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdministradorInvocador_DesactivaParticipante()
    {
        var p = UsuariosDePrueba.NuevoParticipante();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(p.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(p);

        await CrearManejador().Handle(
            new DesactivarParticipanteComando(p.Id), CancellationToken.None);

        p.Estado.Should().Be(EstadoUsuario.Inactivo);
        p.Rol.Should().Be(RolUsuario.Participante);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
