using FluentAssertions;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
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

// El manejador orquesta validador → fábrica/estrategia (incluye generador) →
// Keycloak → repositorio. Las reglas de validación están en
// ValidadorCrearUsuarioPruebas y la generación en GeneradorCodigoUsuarioPruebas.
public class CrearUsuarioManejadorPruebas
{
    private readonly Mock<IRepositorioIdentidad> _repositorio = new();
    private readonly Mock<IProveedorIdentidad> _proveedor = new();
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private readonly Mock<IValidadorCrearUsuario> _validador = new();
    private readonly Mock<IGeneradorCodigoUsuario> _generador = new();
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);

    public CrearUsuarioManejadorPruebas()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);
        _validador
            .Setup(v => v.ValidarAsync(It.IsAny<CrearUsuarioDto>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _generador.Setup(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("OP-001");
        _generador.Setup(g => g.GenerarCodigoAdministradorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("AD-001");
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
            _repositorio.Object, _proveedor.Object, _reloj.Object, fabrica,
            _validador.Object, NullLogger<CrearUsuarioManejador>.Instance);
    }

    private static CrearUsuarioDto Dto(TipoUsuario tipo) => new()
    {
        TipoUsuario = tipo,
        NombreUsuario = "operador01",
        Correo = "operador@umbral.com",
        Contrasena = "Abc1*",
        Nombre = "Ana", Apellido = "Apellido",
        Sexo = "Femenino",
        FechaNacimiento = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DatosContacto = new DatosContactoDto { Direccion = "Calle 1", Telefono = "04143710260" },
        Alias = "ana123"
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
            new CrearUsuarioComando(Dto(TipoUsuario.Operador)), CancellationToken.None);

        resultado.Rol.Should().Be("Operador");
        resultado.Codigo.Should().Be("OP-042");
        resultado.Mensaje.Should().Contain("OP-042");

        _generador.Verify(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.Verify(r => r.GuardarOperadorAsync(
            It.Is<Operador>(o => o.CodigoOperador == "OP-042"),
            "kc-op-x", It.IsAny<CancellationToken>()), Times.Once);
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
            new CrearUsuarioComando(Dto(TipoUsuario.Operador)), CancellationToken.None);

        capturado.Should().NotBeNull();
        capturado!.NombreUsuario.Should().Be("operador01");
        capturado.Correo.Should().Be("operador@umbral.com");
        capturado.Nombre.Should().Be("Ana");
        capturado.Apellido.Should().Be("Apellido");
        capturado.Contrasena.Should().Be("Abc1*");
    }

    [Fact]
    public async Task FlujoAdministrador_DevuelveCodigoGenerado()
    {
        ConfigurarKeycloak("kc-adm-x");
        _generador.Setup(g => g.GenerarCodigoAdministradorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync("AD-007");

        var resultado = await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(TipoUsuario.Administrador)), CancellationToken.None);

        resultado.Rol.Should().Be("Administrador");
        resultado.Codigo.Should().Be("AD-007");

        _generador.Verify(g => g.GenerarCodigoAdministradorAsync(It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.Verify(r => r.GuardarAdministradorAsync(
            It.Is<Administrador>(a => a.CodigoAdministrador == "AD-007"),
            "kc-adm-x", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FlujoParticipante_NoDevuelveCodigo()
    {
        ConfigurarKeycloak("kc-par-x");

        var resultado = await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(TipoUsuario.Participante)), CancellationToken.None);

        resultado.Rol.Should().Be("Participante");
        resultado.Codigo.Should().BeNull();
        _generador.Verify(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        _generador.Verify(g => g.GenerarCodigoAdministradorAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidadorFalla_NoLlamaKeycloakNiGenerador()
    {
        _validador
            .Setup(v => v.ValidarAsync(It.IsAny<CrearUsuarioDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExcepcionValidacion("Existen errores de validación.",
                new[] { new ErrorValidacion("nombreUsuario", "duplicado") }));

        Func<Task> accion = async () => await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(TipoUsuario.Operador)), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
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
        _repositorio.Setup(r => r.GuardarOperadorAsync(
            It.IsAny<Operador>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB caída"));

        Func<Task> accion = async () => await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(TipoUsuario.Operador)), CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>();
        _proveedor.Verify(p => p.EliminarUsuarioAsync("kc-op-x", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FlujoOperador_QuedaActivoPorDefecto()
    {
        ConfigurarKeycloak("kc-op-act");
        Operador? capturado = null;
        _repositorio
            .Setup(r => r.GuardarOperadorAsync(It.IsAny<Operador>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Operador, string, CancellationToken>((o, _, _) => capturado = o)
            .Returns(Task.CompletedTask);

        var resultado = await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(TipoUsuario.Operador)), CancellationToken.None);

        resultado.Estado.Should().Be("Activo");
        capturado!.Estado.Should().Be(EstadoUsuario.Activo);
    }

    [Fact]
    public async Task UsaProveedorFechaHora()
    {
        ConfigurarKeycloak("kc-op-x");

        await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(TipoUsuario.Operador)), CancellationToken.None);

        _reloj.Verify(r => r.ObtenerFechaHoraUtc(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task DtoSinCodigos_NoFalla()
    {
        // El DTO ya no tiene CodigoOperador/CodigoAdministrador: el backend
        // genera siempre el código. Confirma que el flujo funciona aun cuando el
        // frontend no envía esos campos (ahora son inexistentes en el DTO).
        ConfigurarKeycloak("kc-op-x");

        var resultado = await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(TipoUsuario.Operador)), CancellationToken.None);

        resultado.Codigo.Should().Be("OP-001");
    }
}
