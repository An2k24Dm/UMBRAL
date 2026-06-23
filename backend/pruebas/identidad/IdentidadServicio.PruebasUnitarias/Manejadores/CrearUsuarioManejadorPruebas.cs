using FluentAssertions;
using IdentidadServicio.Aplicacion.Comandos.CrearUsuario;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

// HU02 — el manejador depende ahora de puertos segregados:
//  * IRepositorioUnicidadUsuario (duplicados)
//  * IRepositorioOperadores      (alta de Operador)
//  * IRepositorioAdministradores (alta de Administrador)
//  * IUnidadTrabajoIdentidad     (SaveChanges)
// Las pruebas mockean cada interfaz por separado.
public class CrearUsuarioManejadorPruebas
{
    private readonly Mock<IRepositorioUnicidadUsuario> _unicidad = new();
    private readonly Mock<IRepositorioOperadores> _repoOperadores = new();
    private readonly Mock<IRepositorioAdministradores> _repoAdministradores = new();
    private readonly Mock<IUnidadTrabajoIdentidad> _unidad = new();
    private readonly Mock<IProveedorIdentidad> _proveedor = new();
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private readonly Mock<IValidador<CrearUsuarioComando>> _validador = new();
    private readonly Mock<IGeneradorCodigoUsuario> _generador = new();
    private readonly Mock<IGeneradorContrasenaTemporal> _generadorContrasena = new();
    private readonly Mock<IServicioCorreo> _correo = new();
    private readonly Mock<IRepositorioControlContrasenaTemporal> _controlContrasena = new();
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
    private const string ContrasenaTemporalFake = "Temp0r4l*Xyz9";

    public CrearUsuarioManejadorPruebas()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);
        _validador
            .Setup(v => v.Validar(It.IsAny<CrearUsuarioComando>()))
            .Returns(ResultadoValidacion.Exitoso());
        _generador.Setup(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("OP-001");
        _generador.Setup(g => g.GenerarCodigoAdministradorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("AD-001");
        _generadorContrasena.Setup(g => g.Generar()).Returns(ContrasenaTemporalFake);
        _correo.Setup(c => c.EnviarAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private CrearUsuarioManejador CrearManejador()
    {
        var fabrica = new FabricaEstrategiaCreacionUsuario(new IEstrategiaCreacionUsuario[]
        {
            new EstrategiaCrearAdministrador(_generador.Object),
            new EstrategiaCrearOperador(_generador.Object),
            new EstrategiaCrearParticipante()
        });

        return new CrearUsuarioManejador(
            new ValidadorUnicidadUsuario(_unicidad.Object),
            _repoOperadores.Object,
            _repoAdministradores.Object,
            _controlContrasena.Object,
            _unidad.Object,
            _proveedor.Object,
            _reloj.Object,
            fabrica,
            _validador.Object,
            _generadorContrasena.Object,
            _correo.Object,
            NullLogger<CrearUsuarioManejador>.Instance);
    }

    private static CrearUsuarioDto Dto(RolUsuario tipo) => new()
    {
        TipoUsuario = tipo,
        NombreUsuario = "operador01",
        Correo = "operador@umbral.com",
        Nombre = "Ana", Apellido = "Apellido",
        Sexo = "Femenino",
        FechaNacimiento = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DatosContacto = new DatosContactoDto { Direccion = "Calle 1", Telefono = "04143710260" }
    };

    private void ConfigurarKeycloak(string idKc) =>
        _proveedor.Setup(p => p.CrearUsuarioAsync(
            It.IsAny<DatosCreacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(idKc);

    [Fact]
    public async Task FlujoOperador_DevuelveCodigoGenerado()
    {
        ConfigurarKeycloak("kc-op-x");
        _generador.Setup(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("OP-042");

        var resultado = await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(RolUsuario.Operador)), CancellationToken.None);

        resultado.Rol.Should().Be("Operador");
        resultado.Codigo.Should().Be("OP-042");
        resultado.Mensaje.Should().Contain("OP-042");

        _generador.Verify(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _repoOperadores.Verify(r => r.AgregarAsync(
            It.Is<Operador>(o => o.CodigoOperador == "OP-042"),
            "kc-op-x", It.IsAny<CancellationToken>()), Times.Once);
        _unidad.Verify(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FlujoOperador_EnviaNombreYApellidoAlProveedorIdentidad()
    {
        ConfigurarKeycloak("kc-op-x");
        DatosCreacionUsuarioIdentidad? capturado = null;
        _proveedor
            .Setup(p => p.CrearUsuarioAsync(
                It.IsAny<DatosCreacionUsuarioIdentidad>(), It.IsAny<CancellationToken>()))
            .Callback<DatosCreacionUsuarioIdentidad, CancellationToken>((d, _) => capturado = d)
            .ReturnsAsync("kc-op-x");

        await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(RolUsuario.Operador)), CancellationToken.None);

        capturado.Should().NotBeNull();
        capturado!.NombreUsuario.Should().Be("operador01");
        capturado.Correo.Should().Be("operador@umbral.com");
        capturado.Nombre.Should().Be("Ana");
        capturado.Apellido.Should().Be("Apellido");
        // El handler usa una contraseña temporal generada en backend. La
        // credencial se guarda en Keycloak como NO temporal — el control
        // de "obligar cambio" vive en la bandera UMBRAL.
        capturado.Contrasena.Should().Be(ContrasenaTemporalFake);
    }

    [Fact]
    public async Task FlujoAdministrador_DevuelveCodigoGenerado()
    {
        ConfigurarKeycloak("kc-adm-x");
        _generador.Setup(g => g.GenerarCodigoAdministradorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("AD-007");

        var resultado = await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(RolUsuario.Administrador)), CancellationToken.None);

        resultado.Rol.Should().Be("Administrador");
        resultado.Codigo.Should().Be("AD-007");

        _generador.Verify(g => g.GenerarCodigoAdministradorAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _repoAdministradores.Verify(r => r.AgregarAsync(
            It.Is<Administrador>(a => a.CodigoAdministrador == "AD-007"),
            "kc-adm-x", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidadorFalla_NoLlamaKeycloakNiGenerador()
    {
        var resultadoConErrores = ResultadoValidacion.Exitoso();
        resultadoConErrores.Agregar("nombreUsuario", "duplicado");
        _validador
            .Setup(v => v.Validar(It.IsAny<CrearUsuarioComando>()))
            .Returns(resultadoConErrores);

        Func<Task> accion = async () => await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(RolUsuario.Operador)), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
        _proveedor.Verify(p => p.CrearUsuarioAsync(
            It.IsAny<DatosCreacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _generador.Verify(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DuplicadoEnRepositorio_LanzaExcepcionValidacion_AntesDeKeycloak()
    {
        // HU02 — los duplicados ahora se consultan vía IRepositorioUnicidadUsuario.
        _unicidad.Setup(r => r.ExisteCorreoAsync(It.IsAny<string>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(true);

        Func<Task> accion = async () => await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(RolUsuario.Operador)), CancellationToken.None);

        var excepcion = await accion.Should().ThrowAsync<ExcepcionValidacion>();
        excepcion.Which.Errores.Should().Contain(e =>
            e.Campo == MensajesValidacionUsuario.CampoCorreo);
        _proveedor.Verify(p => p.CrearUsuarioAsync(
            It.IsAny<DatosCreacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _generador.Verify(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FalloEnRepositorio_CompensaEliminandoEnKeycloak()
    {
        ConfigurarKeycloak("kc-op-x");
        _unidad.Setup(u => u.GuardarCambiosAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB caída"));

        Func<Task> accion = async () => await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(RolUsuario.Operador)), CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>();
        _proveedor.Verify(p => p.EliminarUsuarioAsync("kc-op-x", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FlujoOperador_QuedaActivoPorDefecto()
    {
        ConfigurarKeycloak("kc-op-act");
        Operador? capturado = null;
        _repoOperadores
            .Setup(r => r.AgregarAsync(It.IsAny<Operador>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Operador, string, CancellationToken>((o, _, _) => capturado = o)
            .Returns(Task.CompletedTask);

        var resultado = await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(RolUsuario.Operador)), CancellationToken.None);

        resultado.Estado.Should().Be("Activo");
        capturado!.Estado.Should().Be(EstadoUsuario.Activo);
    }

    [Fact]
    public async Task UsaProveedorFechaHora()
    {
        ConfigurarKeycloak("kc-op-x");

        await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(RolUsuario.Operador)), CancellationToken.None);

        _reloj.Verify(r => r.ObtenerFechaHoraUtc(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task FlujoOperador_NormalizaFechaNacimientoAUtcSinHora()
    {
        ConfigurarKeycloak("kc-op-utc");
        Operador? capturado = null;
        _repoOperadores
            .Setup(r => r.AgregarAsync(It.IsAny<Operador>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Operador, string, CancellationToken>((o, _, _) => capturado = o)
            .Returns(Task.CompletedTask);

        var dto = Dto(RolUsuario.Operador);
        dto.FechaNacimiento = new DateTime(1990, 1, 15, 13, 45, 0, DateTimeKind.Unspecified);

        await CrearManejador().Handle(new CrearUsuarioComando(dto), CancellationToken.None);

        capturado!.FechaNacimiento.Kind.Should().Be(DateTimeKind.Utc);
        capturado.FechaNacimiento.Should().Be(
            new DateTime(1990, 1, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FlujoAdministrador_NormalizaFechaNacimientoAUtcSinHora()
    {
        ConfigurarKeycloak("kc-adm-utc");
        Administrador? capturado = null;
        _repoAdministradores
            .Setup(r => r.AgregarAsync(It.IsAny<Administrador>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Administrador, string, CancellationToken>((a, _, _) => capturado = a)
            .Returns(Task.CompletedTask);

        var dto = Dto(RolUsuario.Administrador);
        dto.FechaNacimiento = new DateTime(1985, 6, 3, 23, 59, 59, DateTimeKind.Unspecified);

        await CrearManejador().Handle(new CrearUsuarioComando(dto), CancellationToken.None);

        capturado!.FechaNacimiento.Kind.Should().Be(DateTimeKind.Utc);
        capturado.FechaNacimiento.Should().Be(
            new DateTime(1985, 6, 3, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task DtoSinCodigos_NoFalla()
    {
        ConfigurarKeycloak("kc-op-x");

        var resultado = await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(RolUsuario.Operador)), CancellationToken.None);

        resultado.Codigo.Should().Be("OP-001");
    }
}
