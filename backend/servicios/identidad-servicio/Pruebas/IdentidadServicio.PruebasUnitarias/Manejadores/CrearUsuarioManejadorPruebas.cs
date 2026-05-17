using FluentAssertions;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Manejadores;

public class CrearUsuarioManejadorPruebas
{
    private readonly Mock<IRepositorioIdentidad> _repositorio = new();
    private readonly Mock<IProveedorIdentidad> _proveedor = new();
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);

    private CrearUsuarioManejador CrearManejador()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);

        var fabrica = new FabricaEstrategiaCreacionUsuario(new IEstrategiaCreacionUsuario[]
        {
            new EstrategiaCrearAdministrador(),
            new EstrategiaCrearOperador(),
            new EstrategiaCrearParticipante()
        });

        return new CrearUsuarioManejador(
            _repositorio.Object, _proveedor.Object, _reloj.Object, fabrica,
            NullLogger<CrearUsuarioManejador>.Instance);
    }

    private static CrearUsuarioDto Dto(TipoUsuario tipo) => new()
    {
        TipoUsuario = tipo,
        NombreUsuario = "operador01",
        Correo = "operador@umbral.com",
        ContrasenaTemporal = "Temporal123*",
        Nombre = "Ana", Apellido = "Apellido",
        Sexo = "Femenino",
        FechaNacimiento = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DatosContacto = new DatosContactoDto { Direccion = "Calle 1", Telefono = "555" },
        CodigoOperador = "OP-001",
        Alias = "ana123"
    };

    private void ConfigurarKeycloak(string idKc) =>
        _proveedor.Setup(p => p.CrearUsuarioAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(idKc);

    [Fact]
    public async Task FlujoOperador_EnviaUsernameYCorreoSeparadosAKeycloak()
    {
        ConfigurarKeycloak("kc-op-x");

        var resultado = await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(TipoUsuario.Operador)), CancellationToken.None);

        resultado.Rol.Should().Be("Operador");

        // El proveedor recibió username y correo separados (NombreUsuario ≠ Correo).
        _proveedor.Verify(p => p.CrearUsuarioAsync(
            "operador01", "operador@umbral.com", "Temporal123*",
            It.IsAny<CancellationToken>()), Times.Once);

        _proveedor.Verify(p => p.AsignarRolAsync("kc-op-x", "Operador",
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.GuardarOperadorAsync(
            It.Is<Operador>(o => o.CodigoOperador == "OP-001"),
            "kc-op-x", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FlujoParticipante_AsignaRolParticipante()
    {
        ConfigurarKeycloak("kc-par-x");

        await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(TipoUsuario.Participante)), CancellationToken.None);

        _proveedor.Verify(p => p.AsignarRolAsync("kc-par-x", "Participante",
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.GuardarParticipanteAsync(
            It.Is<Participante>(p => p.Alias == "ana123"),
            "kc-par-x", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FlujoAdministrador_AsignaRolAdministrador()
    {
        ConfigurarKeycloak("kc-adm-x");
        var dto = Dto(TipoUsuario.Administrador);
        dto.CodigoAdministrador = "ADM-007";

        await CrearManejador().Handle(
            new CrearUsuarioComando(dto), CancellationToken.None);

        _proveedor.Verify(p => p.AsignarRolAsync("kc-adm-x", "Administrador",
            It.IsAny<CancellationToken>()), Times.Once);
        _repositorio.Verify(r => r.GuardarAdministradorAsync(
            It.Is<Administrador>(a => a.CodigoAdministrador == "ADM-007"),
            "kc-adm-x", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TipoUsuarioInvalido_LanzaRolNoValido_NoLlamaKeycloak()
    {
        Func<Task> accion = async () => await CrearManejador().Handle(
            new CrearUsuarioComando(Dto((TipoUsuario)999)), CancellationToken.None);

        await accion.Should().ThrowAsync<RolNoValidoExcepcion>();
        _proveedor.Verify(p => p.CrearUsuarioAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
    public async Task UsaProveedorFechaHora()
    {
        ConfigurarKeycloak("kc-op-x");

        await CrearManejador().Handle(
            new CrearUsuarioComando(Dto(TipoUsuario.Operador)), CancellationToken.None);

        _reloj.Verify(r => r.ObtenerFechaHoraUtc(), Times.AtLeastOnce);
    }
}
