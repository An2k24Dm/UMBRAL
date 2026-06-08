using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using IdentidadServicio.Dominio.ObjetosDeValor;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

// HU09 — pruebas del coordinador. El manejador ya no detecta cambios ni
// valida duplicados: esas responsabilidades viven en AplicadorCambiosUsuario
// y ValidadorUnicidadModificarOperador respectivamente, que tienen sus
// propios archivos de prueba. Aquí solo cubrimos el flujo de coordinación.
public class ModificarOperadorManejadorPruebas
{
    private readonly Mock<IRepositorioOperadores> _repositorio = new();
    private readonly Mock<IUnidadTrabajoIdentidad> _unidad = new();
    private readonly Mock<IProveedorIdentidad> _proveedor = new();
    private readonly Mock<IValidador<ModificarOperadorComando>> _validador = new();
    private readonly Mock<IValidadorAsincrono<ModificarOperadorComando>> _validadorUnicidad = new();
    private static readonly DateTime FechaRegistroOriginal = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public ModificarOperadorManejadorPruebas()
    {
        _validador
            .Setup(v => v.Validar(It.IsAny<ModificarOperadorComando>()))
            .Returns(ResultadoValidacion.Exitoso());
        _validadorUnicidad
            .Setup(v => v.ValidarAsync(It.IsAny<ModificarOperadorComando>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ResultadoValidacion.Exitoso());
        _repositorio
            .Setup(r => r.ActualizarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-operador");
        _repositorio
            .Setup(r => r.ObtenerIdKeycloakAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("kc-operador");
        _unidad
            .Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static Operador OperadorOriginal(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        NombreUsuario.Crear("operador01"),
        Correo.Crear("operador@umbral.com"),
        EstadoUsuario.Activo,
        FechaRegistroOriginal,
        NombrePersona.Crear("Olivia", "Operadora"),
        DatosContacto.Crear("Av. Libertador, Caracas", "04141111111"),
        SexoPersona.Femenino,
        new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        "OP-001");

    private ModificarOperadorManejador CrearManejador(
        ILogger<ModificarOperadorManejador>? logger = null)
    {
        var fabrica = new FabricaEstrategiaMapeoPerfilUsuario(new IEstrategiaMapeoPerfilUsuario[]
        {
            new EstrategiaMapeoPerfilOperador(),
            new EstrategiaMapeoPerfilAdministrador(),
            new EstrategiaMapeoPerfilParticipante()
        });

        return new ModificarOperadorManejador(
            _repositorio.Object,
            _unidad.Object,
            _proveedor.Object,
            _validador.Object,
            _validadorUnicidad.Object,
            new AplicadorCambiosUsuario(),
            fabrica,
            logger ?? NullLogger<ModificarOperadorManejador>.Instance);
    }

    private void EncolarOperador(Operador op) =>
        _repositorio.Setup(r => r.ObtenerPorIdAsync(op.Id, It.IsAny<CancellationToken>()))
                    .ReturnsAsync(op);

    // ============================================================
    // Coordinación: validadores
    // ============================================================

    [Fact]
    public async Task EjecutaValidadorDeFormato()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, new ModificarOperadorSolicitudDto { Nombre = "Olivia Nueva" }),
            CancellationToken.None);

        _validador.Verify(v => v.Validar(It.IsAny<ModificarOperadorComando>()), Times.Once);
    }

    [Fact]
    public async Task EjecutaValidadorDeUnicidad()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, new ModificarOperadorSolicitudDto { Correo = "x@y.com" }),
            CancellationToken.None);

        _validadorUnicidad.Verify(v =>
            v.ValidarAsync(It.IsAny<ModificarOperadorComando>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidadorFormatoFalla_NoConsultaUnicidadNiRepositorio()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);
        var resultadoFalla = ResultadoValidacion.Exitoso();
        resultadoFalla.Agregar("correo", "inválido");
        _validador.Setup(v => v.Validar(It.IsAny<ModificarOperadorComando>())).Returns(resultadoFalla);

        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, new ModificarOperadorSolicitudDto { Correo = "x" }),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
        _validadorUnicidad.Verify(v =>
            v.ValidarAsync(It.IsAny<ModificarOperadorComando>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repositorio.Verify(r => r.ActualizarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidadorUnicidadFalla_NoLlamaRepositorioNiKeycloak()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);
        var resultadoFalla = ResultadoValidacion.Exitoso();
        resultadoFalla.Agregar("correo", "duplicado");
        _validadorUnicidad
            .Setup(v => v.ValidarAsync(It.IsAny<ModificarOperadorComando>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultadoFalla);

        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, new ModificarOperadorSolicitudDto { Correo = "x@y.com" }),
            CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
        _repositorio.Verify(r => r.ActualizarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ============================================================
    // Coordinación: existencia y "sin cambios"
    // ============================================================

    [Fact]
    public async Task OperadorInexistente_Rechaza()
    {
        var id = Guid.NewGuid();
        _repositorio
            .Setup(r => r.ObtenerPorIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Operador?)null);

        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarOperadorComando(id, new ModificarOperadorSolicitudDto { Nombre = "Otra" }),
            CancellationToken.None);

        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>();
        _repositorio.Verify(r => r.ActualizarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SinCambios_NoPersisteNiLlamaKeycloak()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        // DTO con los mismos valores actuales: AplicadorCambiosUsuario no
        // detecta cambios y el manejador devuelve HuboCambios=false.
        var dto = new ModificarOperadorSolicitudDto
        {
            Correo = "operador@umbral.com",
            Nombre = "Olivia"
        };

        var resultado = await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        resultado.HuboCambios.Should().BeFalse();
        resultado.Mensaje.Should().Contain("No había cambios");
        _repositorio.Verify(r => r.ActualizarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()), Times.Never);
        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ============================================================
    // Coordinación: persistencia + Keycloak
    // ============================================================

    [Fact]
    public async Task CambiaCorreo_ActualizaKeycloakYGuarda()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        var dto = new ModificarOperadorSolicitudDto { Correo = "nuevo@umbral.com" };
        var resultado = await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        resultado.HuboCambios.Should().BeTrue();
        resultado.CamposActualizados.Should().Contain("correo");
        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            "kc-operador",
            It.Is<DatosActualizacionUsuarioIdentidad>(d => d.Correo == "nuevo@umbral.com"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CambiaSoloTelefono_GuardaPeroNoTocaKeycloak()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        var dto = new ModificarOperadorSolicitudDto
        {
            DatosContacto = new DatosContactoDto { Telefono = "04149999999" }
        };
        await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task KeycloakFalla_NoLlamaGuardarCambiosAsync()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);
        _proveedor
            .Setup(p => p.ActualizarUsuarioAsync(
                It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak caído"));

        var dto = new ModificarOperadorSolicitudDto { Correo = "nuevo@umbral.com" };
        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);
        await accion.Should().ThrowAsync<InvalidOperationException>();

        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IdKeycloakVacio_ConCambiosKeycloak_Lanza_YNoGuarda()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);
        _repositorio
            .Setup(r => r.ActualizarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(string.Empty);

        var dto = new ModificarOperadorSolicitudDto { Correo = "nuevo@umbral.com" };
        Func<Task> accion = () => CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);
        await accion.Should().ThrowAsync<InvalidOperationException>().WithMessage("*IdKeycloak*");

        _proveedor.Verify(p => p.ActualizarUsuarioAsync(
            It.IsAny<string>(), It.IsAny<DatosActualizacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PreservaEstadoRolFechaRegistroAlPersistir()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        Operador? capturado = null;
        _repositorio
            .Setup(r => r.ActualizarAsync(It.IsAny<Operador>(), It.IsAny<CancellationToken>()))
            .Callback<Operador, CancellationToken>((o, _) => capturado = o)
            .ReturnsAsync("kc-operador");

        var dto = new ModificarOperadorSolicitudDto { Nombre = "Olivia María" };
        await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        capturado.Should().NotBeNull();
        capturado!.Estado.Should().Be(EstadoUsuario.Activo);
        capturado.Rol.Should().Be(RolUsuario.Operador);
        capturado.FechaRegistro.Should().Be(FechaRegistroOriginal);
    }

    // ============================================================
    // El endpoint de modificación NO acepta cambios de contraseña.
    // El cambio de contraseña vive exclusivamente en el endpoint de
    // reseteo (genera contraseña temporal y la envía por correo).
    // ============================================================

    [Fact]
    public async Task ModificarOperador_NuncaLlamaCambiarContrasenaAsync()
    {
        var original = OperadorOriginal();
        EncolarOperador(original);

        var dto = new ModificarOperadorSolicitudDto { Nombre = "Olivia María" };
        await CrearManejador().Handle(
            new ModificarOperadorComando(original.Id, dto), CancellationToken.None);

        _proveedor.Verify(p => p.CambiarContrasenaAsync(
            It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }
}
