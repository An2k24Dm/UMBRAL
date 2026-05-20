using FluentAssertions;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;

namespace IdentidadServicio.PruebasUnitarias.Dtos;

// Pruebas del DTO de fila de listado de HU08. No hay un mapeador específico
// hacia este DTO en la capa de aplicación (la construcción ocurre dentro del
// repositorio, traduciendo proyecciones EF Core). Estas pruebas validan el
// contrato del DTO: defaults, presencia de Código según rol y ausencia del
// otro código.
public class UsuarioInternoListadoDtoPruebas
{
    private static readonly Guid IdFijo =
        new("11111111-2222-3333-4444-555555555555");

    // -----------------------------------------------------------------
    // Helpers — no usan DateTime.Now ni DateTime.UtcNow.
    // -----------------------------------------------------------------
    private static UsuarioInternoListadoDto NuevaFilaOperador() => new()
    {
        Id = IdFijo,
        Rol = nameof(RolUsuario.Operador),
        NombreUsuario = "operador01",
        Nombre = "Olivia",
        Apellido = "Operadora",
        Estado = nameof(EstadoUsuario.Activo),
        Sexo = nameof(SexoPersona.Femenino),
        CodigoOperador = "OP-001",
        CodigoAdministrador = null
    };

    private static UsuarioInternoListadoDto NuevaFilaAdministrador() => new()
    {
        Id = IdFijo,
        Rol = nameof(RolUsuario.Administrador),
        NombreUsuario = "administrador01",
        Nombre = "Ada",
        Apellido = "Admin",
        Estado = nameof(EstadoUsuario.Activo),
        Sexo = nameof(SexoPersona.Femenino),
        CodigoOperador = null,
        CodigoAdministrador = "AD-001"
    };

    [Fact]
    public void Operador_TieneCodigoOperadorYSinCodigoAdministrador()
    {
        var fila = NuevaFilaOperador();

        fila.Rol.Should().Be("Operador");
        fila.CodigoOperador.Should().Be("OP-001");
        fila.CodigoAdministrador.Should().BeNull();
        fila.NombreUsuario.Should().Be("operador01");
        fila.Nombre.Should().Be("Olivia");
        fila.Apellido.Should().Be("Operadora");
        fila.Estado.Should().Be("Activo");
        fila.Sexo.Should().Be("Femenino");
    }

    [Fact]
    public void Administrador_TieneCodigoAdministradorYSinCodigoOperador()
    {
        var fila = NuevaFilaAdministrador();

        fila.Rol.Should().Be("Administrador");
        fila.CodigoAdministrador.Should().Be("AD-001");
        fila.CodigoOperador.Should().BeNull();
        fila.NombreUsuario.Should().Be("administrador01");
        fila.Nombre.Should().Be("Ada");
        fila.Apellido.Should().Be("Admin");
        fila.Estado.Should().Be("Activo");
        fila.Sexo.Should().Be("Femenino");
    }

    [Fact]
    public void Defaults_DeUsuarioInternoListadoDto_SonSegurosParaSerializacion()
    {
        var fila = new UsuarioInternoListadoDto();

        // Strings nunca nulos por defecto (init = string.Empty).
        fila.NombreUsuario.Should().NotBeNull();
        fila.Nombre.Should().NotBeNull();
        fila.Apellido.Should().NotBeNull();
        fila.Rol.Should().NotBeNull();
        fila.Estado.Should().NotBeNull();
        fila.Sexo.Should().NotBeNull();

        // Códigos opcionales: null por defecto.
        fila.CodigoOperador.Should().BeNull();
        fila.CodigoAdministrador.Should().BeNull();

        fila.Id.Should().Be(Guid.Empty);
    }

    [Fact]
    public void ResultadoPaginado_VacioPorDefecto_TienePropiedadesConsistentes()
    {
        var resultado = new ResultadoPaginadoDto<UsuarioInternoListadoDto>
        {
            Elementos = Array.Empty<UsuarioInternoListadoDto>(),
            Pagina = 1,
            TamanioPagina = 10,
            Total = 0
        };

        resultado.Elementos.Should().BeEmpty();
        resultado.Pagina.Should().Be(1);
        resultado.TamanioPagina.Should().Be(10);
        resultado.Total.Should().Be(0);
    }

    [Fact]
    public void ResultadoPaginado_ConservaElementos()
    {
        var elementos = new[] { NuevaFilaOperador(), NuevaFilaAdministrador() };

        var resultado = new ResultadoPaginadoDto<UsuarioInternoListadoDto>
        {
            Elementos = elementos,
            Pagina = 1,
            TamanioPagina = 10,
            Total = 2
        };

        resultado.Elementos.Should().HaveCount(2);
        resultado.Elementos.Should().Contain(e => e.Rol == "Operador");
        resultado.Elementos.Should().Contain(e => e.Rol == "Administrador");
        resultado.Elementos.Should().NotContain(e => e.Rol == "Participante");
    }
}
