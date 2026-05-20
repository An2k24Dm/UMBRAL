using FluentAssertions;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Estrategias;

public class EstrategiasPruebas
{
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Nacimiento = new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CrearUsuarioDto DtoBase(RolUsuario tipo) => new()
    {
        TipoUsuario = tipo,
        NombreUsuario = "usuario01",
        Correo = "usuario@umbral.com",
        Contrasena = "Abc1*",
        Nombre = "Ana",
        Apellido = "Apellido",
        Sexo = "Femenino",
        FechaNacimiento = Nacimiento,
        DatosContacto = new DatosContactoDto { Direccion = "Calle 1", Telefono = "04143710260" }
    };

    private static Mock<IGeneradorCodigoUsuario> GeneradorConCodigos(string operador, string admin)
    {
        var mock = new Mock<IGeneradorCodigoUsuario>();
        mock.Setup(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(operador);
        mock.Setup(g => g.GenerarCodigoAdministradorAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(admin);
        return mock;
    }

    [Fact]
    public async Task EstrategiaCrearAdministrador_UsaGeneradorParaCodigo()
    {
        var generador = GeneradorConCodigos("OP-IGNORADO", "AD-007");

        var usuario = await new EstrategiaCrearAdministrador(generador.Object)
            .CrearUsuarioDominioAsync(DtoBase(RolUsuario.Administrador), Ahora, default);

        usuario.Should().BeOfType<Administrador>();
        usuario.Rol.Should().Be(RolUsuario.Administrador);
        ((Administrador)usuario).CodigoAdministrador.Should().Be("AD-007");
        generador.Verify(g => g.GenerarCodigoAdministradorAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EstrategiaCrearOperador_UsaGeneradorParaCodigo()
    {
        var generador = GeneradorConCodigos("OP-042", "AD-IGNORADO");

        var usuario = await new EstrategiaCrearOperador(generador.Object)
            .CrearUsuarioDominioAsync(DtoBase(RolUsuario.Operador), Ahora, default);

        usuario.Should().BeOfType<Operador>();
        ((Operador)usuario).CodigoOperador.Should().Be("OP-042");
        generador.Verify(g => g.GenerarCodigoOperadorAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task EstrategiaCrearOperador_NoNecesitaCodigoEnDto()
    {
        // El DTO no expone CodigoOperador: la estrategia genera siempre el código.
        var generador = GeneradorConCodigos("OP-001", "AD-001");
        var dto = DtoBase(RolUsuario.Operador);

        var usuario = await new EstrategiaCrearOperador(generador.Object)
            .CrearUsuarioDominioAsync(dto, Ahora, default);

        ((Operador)usuario).CodigoOperador.Should().Be("OP-001");
    }

    [Fact]
    public async Task EstrategiaCrearParticipante_CreaParticipanteConAlias()
    {
        var dto = DtoBase(RolUsuario.Participante);
        dto.Alias = "ana123";

        var usuario = await new EstrategiaCrearParticipante()
            .CrearUsuarioDominioAsync(dto, Ahora, default);

        usuario.Should().BeOfType<Participante>();
        ((Participante)usuario).Alias.Should().Be("ana123");
    }

    [Fact]
    public async Task EstrategiaCrearParticipante_SinAlias_Lanza()
    {
        Func<Task> accion = async () => await new EstrategiaCrearParticipante()
            .CrearUsuarioDominioAsync(DtoBase(RolUsuario.Participante), Ahora, default);
        await accion.Should().ThrowAsync<DatosUsuarioInvalidosExcepcion>();
    }
}
