using FluentAssertions;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.PruebasUnitarias.Estrategias;

public class EstrategiasPruebas
{
    private static readonly DateTime Ahora = new(2026, 5, 17, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Nacimiento = new(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static CrearUsuarioDto DtoBase(TipoUsuario tipo) => new()
    {
        TipoUsuario = tipo,
        NombreUsuario = "usuario01",
        Correo = "usuario@umbral.com",
        ContrasenaTemporal = "Temporal123*",
        Nombre = "Ana",
        Apellido = "Apellido",
        Sexo = "Femenino",
        FechaNacimiento = Nacimiento,
        DatosContacto = new DatosContactoDto { Direccion = "Calle 1", Telefono = "555" }
    };

    [Fact]
    public void EstrategiaCrearAdministrador_CreaAdministrador()
    {
        var dto = DtoBase(TipoUsuario.Administrador);
        dto.CodigoAdministrador = "ADM-001";

        var usuario = new EstrategiaCrearAdministrador().CrearUsuarioDominio(dto, Ahora);

        usuario.Should().BeOfType<Administrador>();
        usuario.Rol.Should().Be(RolUsuario.Administrador);
        usuario.Correo.Valor.Should().Be("usuario@umbral.com");
        usuario.NombreUsuario.Valor.Should().Be("usuario01");
        ((Administrador)usuario).CodigoAdministrador.Should().Be("ADM-001");
    }

    [Fact]
    public void EstrategiaCrearOperador_CreaOperadorConCodigo()
    {
        var dto = DtoBase(TipoUsuario.Operador);
        dto.CodigoOperador = "OP-001";

        var usuario = new EstrategiaCrearOperador().CrearUsuarioDominio(dto, Ahora);

        usuario.Should().BeOfType<Operador>();
        ((Operador)usuario).CodigoOperador.Should().Be("OP-001");
    }

    [Fact]
    public void EstrategiaCrearOperador_SinCodigo_Lanza()
    {
        Action accion = () => new EstrategiaCrearOperador()
            .CrearUsuarioDominio(DtoBase(TipoUsuario.Operador), Ahora);
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }

    [Fact]
    public void EstrategiaCrearParticipante_CreaParticipanteConAlias()
    {
        var dto = DtoBase(TipoUsuario.Participante);
        dto.Alias = "ana123";

        var usuario = new EstrategiaCrearParticipante().CrearUsuarioDominio(dto, Ahora);

        usuario.Should().BeOfType<Participante>();
        ((Participante)usuario).Alias.Should().Be("ana123");
    }

    [Fact]
    public void EstrategiaCrearParticipante_SinAlias_Lanza()
    {
        Action accion = () => new EstrategiaCrearParticipante()
            .CrearUsuarioDominio(DtoBase(TipoUsuario.Participante), Ahora);
        accion.Should().Throw<DatosUsuarioInvalidosExcepcion>();
    }
}
