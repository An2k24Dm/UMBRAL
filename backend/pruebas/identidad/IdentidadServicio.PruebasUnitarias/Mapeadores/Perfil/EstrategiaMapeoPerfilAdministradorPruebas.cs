using FluentAssertions;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;

public class EstrategiaMapeoPerfilAdministradorPruebas
{
    private static EstrategiaMapeoPerfilAdministrador CrearEstrategia() => new();

    [Fact]
    public void PuedeMapear_ConAdministrador_DevuelveTrue()
    {
        CrearEstrategia().PuedeMapear(UsuariosDePrueba.NuevoAdministrador())
            .Should().BeTrue();
    }

    [Fact]
    public void PuedeMapear_ConOperador_DevuelveFalse()
    {
        CrearEstrategia().PuedeMapear(UsuariosDePrueba.NuevoOperador())
            .Should().BeFalse();
    }

    [Fact]
    public void PuedeMapear_ConParticipante_DevuelveFalse()
    {
        CrearEstrategia().PuedeMapear(UsuariosDePrueba.NuevoParticipante())
            .Should().BeFalse();
    }

    [Fact]
    public void Mapear_DevuelveInstanciaDePerfilAdministradorDto()
    {
        var administrador = UsuariosDePrueba.NuevoAdministrador();
        var dto = CrearEstrategia().Mapear(administrador);
        dto.Should().BeOfType<PerfilAdministradorDto>();
    }

    [Fact]
    public void Mapear_AsignaCodigoAdministrador()
    {
        var administrador = UsuariosDePrueba.NuevoAdministrador(codigoAdministrador: "AD-007");
        var dto = (PerfilAdministradorDto)CrearEstrategia().Mapear(administrador);
        dto.CodigoAdministrador.Should().Be("AD-007");
    }

    [Fact]
    public void Mapear_AsignaDatosComunes()
    {
        var administrador = UsuariosDePrueba.NuevoAdministrador();
        var dto = CrearEstrategia().Mapear(administrador);

        dto.Id.Should().Be(administrador.Id);
        dto.NombreUsuario.Should().Be(administrador.NombreUsuario.Valor);
        dto.Correo.Should().Be(administrador.Correo.Valor);
        dto.Rol.Should().Be("Administrador");
        dto.Estado.Should().Be("Activo");
        dto.Nombre.Should().Be(administrador.NombrePersona.Nombre);
        dto.Apellido.Should().Be(administrador.NombrePersona.Apellido);
        dto.Sexo.Should().Be(administrador.Sexo.ToString());
        dto.FechaNacimiento.Should().Be(administrador.FechaNacimiento);
    }

    [Fact]
    public void Mapear_AsignaDatosContactoAnidados()
    {
        var administrador = UsuariosDePrueba.NuevoAdministrador();
        var dto = CrearEstrategia().Mapear(administrador);

        dto.DatosContacto.Should().NotBeNull();
        dto.DatosContacto.Direccion.Should().Be(UsuariosDePrueba.Direccion);
        dto.DatosContacto.Telefono.Should().Be(UsuariosDePrueba.Telefono);
    }
}
