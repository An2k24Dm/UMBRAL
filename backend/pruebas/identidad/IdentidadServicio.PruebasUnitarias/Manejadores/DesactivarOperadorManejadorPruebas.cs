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

// HU12 — pruebas del manejador de desactivación de Operador. Cubren:
//  * Sólo Administrador Activo puede invocar.
//  * 404 si el Operador no existe o no es Operador.
//  * 400 USUARIO_YA_INACTIVO si ya está Inactivo (idempotencia controlada).
//  * Llama Desactivar() del dominio.
//  * Sólo escribe Estado (ActualizarEstadoAsync) y confirma con la unidad
//    de trabajo.
//  * Nunca llama a IProveedorIdentidad.EliminarUsuarioAsync (Keycloak intacto).
public class DesactivarOperadorManejadorPruebas
{
    private readonly Mock<IAutorizadorUsuarioActivo> _autorizador = new();
    private readonly Mock<IRepositorioOperadores> _repositorio = new();
    private readonly Mock<IUnidadTrabajoIdentidad> _unidad = new();

    public DesactivarOperadorManejadorPruebas()
    {
        // Por defecto el invocador es un Administrador Activo aceptado.
        _autorizador
            .Setup(a => a.RequerirRolActivoAsync(RolUsuario.Administrador, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UsuariosDePrueba.NuevoAdministrador());
        _repositorio
            .Setup(r => r.ActualizarEstadoAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _unidad
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private DesactivarOperadorManejador CrearManejador()
        => new(
            _autorizador.Object,
            _repositorio.Object,
            _unidad.Object,
            NullLogger<DesactivarOperadorManejador>.Instance);

    [Fact]
    public async Task InvocadorNoAdministrador_LanzaAccesoNoPermitido()
    {
        _autorizador
            .Setup(a => a.RequerirRolActivoAsync(RolUsuario.Administrador, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesoNoPermitidoExcepcion("Su rol no le permite realizar esta acción."));

        Func<Task> accion = () => CrearManejador().Handle(
            new DesactivarOperadorComando(Guid.NewGuid()), CancellationToken.None);
        await accion.Should().ThrowAsync<AccesoNoPermitidoExcepcion>();

        _repositorio.Verify(r => r.ActualizarEstadoAsync(
            It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AdministradorInactivo_LanzaCuentaDesactivada()
    {
        _autorizador
            .Setup(a => a.RequerirRolActivoAsync(RolUsuario.Administrador, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CuentaDesactivadaExcepcion());

        Func<Task> accion = () => CrearManejador().Handle(
            new DesactivarOperadorComando(Guid.NewGuid()), CancellationToken.None);
        await accion.Should().ThrowAsync<CuentaDesactivadaExcepcion>();
    }

    [Fact]
    public async Task OperadorInexistente_Retorna404Controlado()
    {
        var id = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Operador?)null);

        Func<Task> accion = () => CrearManejador().Handle(
            new DesactivarOperadorComando(id), CancellationToken.None);
        (await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>())
            .Which.Message.Should().Contain("No existe un Operador");
    }

    [Fact]
    public async Task OperadorYaInactivo_LanzaUsuarioYaInactivo()
    {
        var op = UsuariosDePrueba.NuevoOperador();
        op.Desactivar();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(op.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(op);

        Func<Task> accion = () => CrearManejador().Handle(
            new DesactivarOperadorComando(op.Id), CancellationToken.None);
        await accion.Should().ThrowAsync<UsuarioYaInactivoExcepcion>();

        _repositorio.Verify(r => r.ActualizarEstadoAsync(
            It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DesactivaOperadorYGuardaCambios()
    {
        var op = UsuariosDePrueba.NuevoOperador();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(op.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(op);

        var resultado = await CrearManejador().Handle(
            new DesactivarOperadorComando(op.Id), CancellationToken.None);

        op.Estado.Should().Be(EstadoUsuario.Inactivo);
        resultado.IdUsuario.Should().Be(op.Id);
        resultado.Estado.Should().Be("Inactivo");
        _repositorio.Verify(r => r.ActualizarEstadoAsync(op, It.IsAny<CancellationToken>()),
            Times.Once);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoModificaRolNiFechaRegistroNiDatosPersonales()
    {
        var op = UsuariosDePrueba.NuevoOperador(
            nombreUsuario: "olivia_op", correo: "olivia@umbral.com",
            nombre: "Olivia", apellido: "Operadora", codigoOperador: "OP-001");
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(op.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(op);

        await CrearManejador().Handle(
            new DesactivarOperadorComando(op.Id), CancellationToken.None);

        op.Rol.Should().Be(RolUsuario.Operador);
        op.NombreUsuario.Valor.Should().Be("olivia_op");
        op.Correo.Valor.Should().Be("olivia@umbral.com");
        op.NombrePersona.Nombre.Should().Be("Olivia");
        op.CodigoOperador.Should().Be("OP-001");
    }
}
