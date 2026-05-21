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

// HU03 — el manejador del registro público de Participante orquesta:
// validador → fábrica/estrategia (Participante) → Keycloak → repositorio.
// Reutiliza EstrategiaCrearParticipante para no duplicar la creación del
// agregado de dominio.
public class RegistrarParticipanteManejadorPruebas
{
    private readonly Mock<IRepositorioIdentidad> _repositorio = new();
    private readonly Mock<IProveedorIdentidad> _proveedor = new();
    private readonly Mock<IProveedorFechaHora> _reloj = new();
    private readonly Mock<IValidador<RegistrarParticipanteDto>> _validador = new();
    private readonly Mock<IGeneradorCodigoUsuario> _generador = new();
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);

    public RegistrarParticipanteManejadorPruebas()
    {
        _reloj.Setup(r => r.ObtenerFechaHoraUtc()).Returns(Ahora);
        _validador
            .Setup(v => v.ValidarAsync(It.IsAny<RegistrarParticipanteDto>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private RegistrarParticipanteManejador CrearManejador()
    {
        var fabrica = new FabricaEstrategiaCreacionUsuario(new IEstrategiaCreacionUsuario[]
        {
            new EstrategiaCrearAdministrador(_generador.Object),
            new EstrategiaCrearOperador(_generador.Object),
            new EstrategiaCrearParticipante()
        });

        return new RegistrarParticipanteManejador(
            _repositorio.Object, _proveedor.Object, _reloj.Object, fabrica,
            _validador.Object, NullLogger<RegistrarParticipanteManejador>.Instance);
    }

    private static RegistrarParticipanteDto Dto() => new()
    {
        Alias = "sombra01",
        NombreUsuario = "participante01",
        Correo = "participante01@umbral.com",
        Contrasena = "Abc1*",
        Nombre = "Pablo",
        Apellido = "Participante",
        Sexo = "Masculino",
        FechaNacimiento = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        DatosContacto = new DatosContactoDto
        {
            Direccion = "Av. Bolívar, Caracas",
            Telefono = "04143710260"
        }
    };

    private void ConfigurarKeycloak(string idKc) =>
        _proveedor.Setup(p => p.CrearUsuarioAsync(
            It.IsAny<DatosCreacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>())).ReturnsAsync(idKc);

    [Fact]
    public async Task FlujoFeliz_CreaParticipanteYDevuelveRespuestaSinCodigo()
    {
        ConfigurarKeycloak("kc-par-x");

        var resultado = await CrearManejador().Handle(
            new RegistrarParticipanteComando(Dto()), CancellationToken.None);

        resultado.Rol.Should().Be("Participante");
        resultado.Estado.Should().Be("Activo");
        resultado.Codigo.Should().BeNull();
        resultado.NombreUsuario.Should().Be("participante01");
        resultado.Correo.Should().Be("participante01@umbral.com");
    }

    [Fact]
    public async Task FlujoFeliz_CreaUsuarioEnKeycloakConDatosCanonicos()
    {
        ConfigurarKeycloak("kc-par-y");
        DatosCreacionUsuarioIdentidad? capturado = null;
        _proveedor
            .Setup(p => p.CrearUsuarioAsync(
                It.IsAny<DatosCreacionUsuarioIdentidad>(), It.IsAny<CancellationToken>()))
            .Callback<DatosCreacionUsuarioIdentidad, CancellationToken>((d, _) => capturado = d)
            .ReturnsAsync("kc-par-y");

        await CrearManejador().Handle(
            new RegistrarParticipanteComando(Dto()), CancellationToken.None);

        capturado.Should().NotBeNull();
        capturado!.NombreUsuario.Should().Be("participante01");
        capturado.Correo.Should().Be("participante01@umbral.com");
        capturado.Nombre.Should().Be("Pablo");
        capturado.Apellido.Should().Be("Participante");
        capturado.Contrasena.Should().Be("Abc1*");
    }

    [Fact]
    public async Task FlujoFeliz_AsignaRolParticipanteEnKeycloak()
    {
        ConfigurarKeycloak("kc-par-z");

        await CrearManejador().Handle(
            new RegistrarParticipanteComando(Dto()), CancellationToken.None);

        _proveedor.Verify(p => p.AsignarRolAsync(
            "kc-par-z",
            nameof(RolUsuario.Participante),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FlujoFeliz_GuardaParticipanteConAliasYIdKeycloak()
    {
        ConfigurarKeycloak("kc-par-save");
        Participante? capturado = null;
        string? idKeycloakCapturado = null;
        _repositorio
            .Setup(r => r.GuardarParticipanteAsync(
                It.IsAny<Participante>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Participante, string, CancellationToken>((p, id, _) =>
            {
                capturado = p;
                idKeycloakCapturado = id;
            })
            .Returns(Task.CompletedTask);

        await CrearManejador().Handle(
            new RegistrarParticipanteComando(Dto()), CancellationToken.None);

        capturado.Should().NotBeNull();
        capturado!.Alias.Should().Be("sombra01");
        capturado.Rol.Should().Be(RolUsuario.Participante);
        idKeycloakCapturado.Should().Be("kc-par-save");
    }

    [Fact]
    public async Task FlujoFeliz_NoConsultaGeneradorDeCodigos()
    {
        // HU03 no usa códigos OP-### / AD-###: la respuesta llega con Codigo = null.
        ConfigurarKeycloak("kc-par-x");

        await CrearManejador().Handle(
            new RegistrarParticipanteComando(Dto()), CancellationToken.None);

        _generador.Verify(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()),
            Times.Never);
        _generador.Verify(g => g.GenerarCodigoAdministradorAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ValidadorFalla_NoLlamaKeycloakNiRepositorio()
    {
        _validador
            .Setup(v => v.ValidarAsync(It.IsAny<RegistrarParticipanteDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ExcepcionValidacion("Existen errores de validación.",
                new[] { new ErrorValidacion("alias", "duplicado") }));

        Func<Task> accion = async () => await CrearManejador().Handle(
            new RegistrarParticipanteComando(Dto()), CancellationToken.None);

        await accion.Should().ThrowAsync<ExcepcionValidacion>();
        _proveedor.Verify(p => p.CrearUsuarioAsync(
            It.IsAny<DatosCreacionUsuarioIdentidad>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _repositorio.Verify(r => r.GuardarParticipanteAsync(
            It.IsAny<Participante>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FalloEnRepositorio_CompensaEliminandoEnKeycloak()
    {
        ConfigurarKeycloak("kc-par-fail");
        _repositorio.Setup(r => r.GuardarParticipanteAsync(
            It.IsAny<Participante>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB caída"));

        Func<Task> accion = async () => await CrearManejador().Handle(
            new RegistrarParticipanteComando(Dto()), CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>();
        _proveedor.Verify(p => p.EliminarUsuarioAsync("kc-par-fail", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task FalloEnAsignarRol_CompensaEliminandoEnKeycloak()
    {
        ConfigurarKeycloak("kc-par-rol");
        _proveedor.Setup(p => p.AsignarRolAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Keycloak rechazó el rol"));

        Func<Task> accion = async () => await CrearManejador().Handle(
            new RegistrarParticipanteComando(Dto()), CancellationToken.None);

        await accion.Should().ThrowAsync<InvalidOperationException>();
        _proveedor.Verify(p => p.EliminarUsuarioAsync("kc-par-rol", It.IsAny<CancellationToken>()),
            Times.Once);
        _repositorio.Verify(r => r.GuardarParticipanteAsync(
            It.IsAny<Participante>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UsaProveedorFechaHora()
    {
        ConfigurarKeycloak("kc-par-x");

        await CrearManejador().Handle(
            new RegistrarParticipanteComando(Dto()), CancellationToken.None);

        _reloj.Verify(r => r.ObtenerFechaHoraUtc(), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ParticipanteQuedaActivoYConFechaRegistroDelReloj()
    {
        ConfigurarKeycloak("kc-par-x");
        Participante? capturado = null;
        _repositorio
            .Setup(r => r.GuardarParticipanteAsync(It.IsAny<Participante>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Participante, string, CancellationToken>((p, _, _) => capturado = p)
            .Returns(Task.CompletedTask);

        var resultado = await CrearManejador().Handle(
            new RegistrarParticipanteComando(Dto()), CancellationToken.None);

        resultado.Estado.Should().Be("Activo");
        capturado!.Estado.Should().Be(EstadoUsuario.Activo);
        capturado.FechaRegistro.Should().Be(Ahora);
    }

    [Fact]
    public async Task NormalizaFechaNacimientoAUtcSinHora()
    {
        // La app móvil envía "fechaNacimiento": "1990-01-15" y ASP.NET la
        // deserializa con Kind=Unspecified. El manejador debe convertirla a UTC
        // (sin hora) antes de cruzar a dominio/persistencia para que Npgsql no
        // rechace la inserción en la columna timestamptz.
        ConfigurarKeycloak("kc-par-utc");
        Participante? capturado = null;
        _repositorio
            .Setup(r => r.GuardarParticipanteAsync(It.IsAny<Participante>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Participante, string, CancellationToken>((p, _, _) => capturado = p)
            .Returns(Task.CompletedTask);

        var dto = Dto();
        dto.FechaNacimiento = new DateTime(1990, 1, 15, 10, 30, 0, DateTimeKind.Unspecified);

        await CrearManejador().Handle(
            new RegistrarParticipanteComando(dto), CancellationToken.None);

        capturado!.FechaNacimiento.Kind.Should().Be(DateTimeKind.Utc);
        capturado.FechaNacimiento.Should().Be(
            new DateTime(1990, 1, 15, 0, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task FechaNacimiento_PreservaElDiaIndependienteDelKindOriginal()
    {
        // No debe haber corrimiento de día por interpretarse en otra zona horaria.
        ConfigurarKeycloak("kc-par-dia");
        Participante? capturado = null;
        _repositorio
            .Setup(r => r.GuardarParticipanteAsync(It.IsAny<Participante>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<Participante, string, CancellationToken>((p, _, _) => capturado = p)
            .Returns(Task.CompletedTask);

        var dto = Dto();
        dto.FechaNacimiento = new DateTime(2002, 12, 31, 23, 59, 59, DateTimeKind.Unspecified);

        await CrearManejador().Handle(
            new RegistrarParticipanteComando(dto), CancellationToken.None);

        capturado!.FechaNacimiento.Year.Should().Be(2002);
        capturado.FechaNacimiento.Month.Should().Be(12);
        capturado.FechaNacimiento.Day.Should().Be(31);
        capturado.FechaNacimiento.Kind.Should().Be(DateTimeKind.Utc);
    }
}
