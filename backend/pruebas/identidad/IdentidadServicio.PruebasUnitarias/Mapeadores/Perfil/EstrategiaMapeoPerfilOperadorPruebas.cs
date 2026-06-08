using FluentAssertions;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;

public class EstrategiaMapeoPerfilOperadorPruebas
{
    private static EstrategiaMapeoPerfilOperador CrearEstrategia() => new();

    [Fact]
    public void PuedeMapear_ConOperador_DevuelveTrue()
    {
        CrearEstrategia().PuedeMapear(UsuariosDePrueba.NuevoOperador())
            .Should().BeTrue();
    }

    [Fact]
    public void PuedeMapear_ConAdministrador_DevuelveFalse()
    {
        CrearEstrategia().PuedeMapear(UsuariosDePrueba.NuevoAdministrador())
            .Should().BeFalse();
    }

    [Fact]
    public void PuedeMapear_ConParticipante_DevuelveFalse()
    {
        CrearEstrategia().PuedeMapear(UsuariosDePrueba.NuevoParticipante())
            .Should().BeFalse();
    }

    [Fact]
    public void Mapear_DevuelveInstanciaDePerfilOperadorDto()
    {
        var operador = UsuariosDePrueba.NuevoOperador();
        var dto = CrearEstrategia().Mapear(operador);
        dto.Should().BeOfType<PerfilOperadorDto>();
    }

    [Fact]
    public void Mapear_AsignaCodigoOperador()
    {
        var operador = UsuariosDePrueba.NuevoOperador(codigoOperador: "OP-042");
        var dto = (PerfilOperadorDto)CrearEstrategia().Mapear(operador);
        dto.CodigoOperador.Should().Be("OP-042");
    }

    [Fact]
    public void Mapear_AsignaDatosComunes()
    {
        var operador = UsuariosDePrueba.NuevoOperador();
        var dto = CrearEstrategia().Mapear(operador);

        dto.Id.Should().Be(operador.Id);
        dto.NombreUsuario.Should().Be(operador.NombreUsuario.Valor);
        dto.Correo.Should().Be(operador.Correo.Valor);
        dto.Rol.Should().Be("Operador");
        dto.Estado.Should().Be("Activo");
        dto.Nombre.Should().Be(operador.NombrePersona.Nombre);
        dto.Apellido.Should().Be(operador.NombrePersona.Apellido);
        dto.Sexo.Should().Be(operador.Sexo.ToString());
        dto.FechaNacimiento.Should().Be(operador.FechaNacimiento);
    }

    [Fact]
    public void Mapear_AsignaDatosContactoAnidados()
    {
        var operador = UsuariosDePrueba.NuevoOperador();
        var dto = CrearEstrategia().Mapear(operador);

        dto.DatosContacto.Should().NotBeNull();
        dto.DatosContacto.Direccion.Should().Be(UsuariosDePrueba.Direccion);
        dto.DatosContacto.Telefono.Should().Be(UsuariosDePrueba.Telefono);
    }
}
