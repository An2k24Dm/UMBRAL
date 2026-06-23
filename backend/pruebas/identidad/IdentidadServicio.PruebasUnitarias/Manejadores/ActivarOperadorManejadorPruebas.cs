using IdentidadServicio.Aplicacion.Comandos.ActivarOperador;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

// Pruebas del manejador de reactivación de Operador. Simétricas a las
// de DesactivarOperador: validan que solo un Administrador Activo pueda
// invocar, que la cuenta esté Inactiva para poder activarse, y que la
// implementación no toque Keycloak ni datos personales.
public class ActivarOperadorManejadorPruebas
{
    private readonly Mock<IAutorizadorUsuarioActivo> _autorizador = new();
    private readonly Mock<IRepositorioOperadores> _repositorio = new();
    private readonly Mock<IUnidadTrabajoIdentidad> _unidad = new();
    private readonly Mock<IProveedorIdentidad> _proveedor = new();

    public ActivarOperadorManejadorPruebas()
    {
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

    private ActivarOperadorManejador CrearManejador()
        => new(
            _autorizador.Object,
            _repositorio.Object,
            _unidad.Object,
            NullLogger<ActivarOperadorManejador>.Instance);

    private static Operador OperadorInactivo()
    {
        var op = UsuariosDePrueba.NuevoOperador();
        op.Desactivar();
        return op;
    }

    [Fact]
    public async Task InvocadorNoAdministrador_LanzaAccesoNoPermitido()
    {
        _autorizador
            .Setup(a => a.RequerirRolActivoAsync(RolUsuario.Administrador, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AccesoNoPermitidoExcepcion("Su rol no le permite realizar esta acción."));

        Func<Task> accion = () => CrearManejador().Handle(
            new ActivarOperadorComando(Guid.NewGuid()), CancellationToken.None);
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
            new ActivarOperadorComando(Guid.NewGuid()), CancellationToken.None);
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
            new ActivarOperadorComando(id), CancellationToken.None);
        (await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>())
            .Which.Message.Should().Contain("No existe un Operador");
    }

    [Fact]
    public async Task OperadorYaActivo_LanzaUsuarioYaActivo()
    {
        var op = UsuariosDePrueba.NuevoOperador(); // Activo por defecto
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(op.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(op);

        Func<Task> accion = () => CrearManejador().Handle(
            new ActivarOperadorComando(op.Id), CancellationToken.None);
        await accion.Should().ThrowAsync<UsuarioYaActivoExcepcion>();

        _repositorio.Verify(r => r.ActualizarEstadoAsync(
            It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OperadorInactivo_PasaAActivoYGuardaCambios()
    {
        var op = OperadorInactivo();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(op.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(op);

        var resultado = await CrearManejador().Handle(
            new ActivarOperadorComando(op.Id), CancellationToken.None);

        op.Estado.Should().Be(EstadoUsuario.Activo);
        resultado.IdUsuario.Should().Be(op.Id);
        resultado.Estado.Should().Be("Activo");
        _repositorio.Verify(r => r.ActualizarEstadoAsync(op, It.IsAny<CancellationToken>()),
            Times.Once);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NoModificaRolNiDatosPersonalesNiLlamaKeycloak()
    {
        var op = UsuariosDePrueba.NuevoOperador(
            nombreUsuario: "olivia_op", correo: "olivia@umbral.com",
            nombre: "Olivia", apellido: "Operadora", codigoOperador: "OP-001");
        op.Desactivar();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(op.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(op);

        await CrearManejador().Handle(
            new ActivarOperadorComando(op.Id), CancellationToken.None);

        op.Rol.Should().Be(RolUsuario.Operador);
        op.NombreUsuario.Valor.Should().Be("olivia_op");
        op.Correo.Valor.Should().Be("olivia@umbral.com");
        op.NombrePersona.Nombre.Should().Be("Olivia");
        op.CodigoOperador.Should().Be("OP-001");

        // No se llama ningún método del proveedor de identidad (Keycloak).
        _proveedor.VerifyNoOtherCalls();
    }
}
