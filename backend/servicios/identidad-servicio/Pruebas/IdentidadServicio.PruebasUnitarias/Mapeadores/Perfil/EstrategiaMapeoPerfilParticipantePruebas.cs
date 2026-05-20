using FluentAssertions;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Commons.Dtos;

namespace IdentidadServicio.PruebasUnitarias.Mapeadores.Perfil;

public class EstrategiaMapeoPerfilParticipantePruebas
{
    private static EstrategiaMapeoPerfilParticipante CrearEstrategia() => new();

    [Fact]
    public void PuedeMapear_ConParticipante_DevuelveTrue()
    {
        CrearEstrategia().PuedeMapear(UsuariosDePrueba.NuevoParticipante())
            .Should().BeTrue();
    }

    [Fact]
    public void PuedeMapear_ConOperador_DevuelveFalse()
    {
        CrearEstrategia().PuedeMapear(UsuariosDePrueba.NuevoOperador())
            .Should().BeFalse();
    }

    [Fact]
    public void PuedeMapear_ConAdministrador_DevuelveFalse()
    {
        CrearEstrategia().PuedeMapear(UsuariosDePrueba.NuevoAdministrador())
            .Should().BeFalse();
    }

    [Fact]
    public void Mapear_DevuelveInstanciaDePerfilParticipanteDto()
    {
        var participante = UsuariosDePrueba.NuevoParticipante();
        var dto = CrearEstrategia().Mapear(participante);
        dto.Should().BeOfType<PerfilParticipanteDto>();
    }

    [Fact]
    public void Mapear_AsignaAlias()
    {
        var participante = UsuariosDePrueba.NuevoParticipante(alias: "pablito_99");
        var dto = (PerfilParticipanteDto)CrearEstrategia().Mapear(participante);
        dto.Alias.Should().Be("pablito_99");
    }

    [Fact]
    public void Mapear_AsignaDatosComunes()
    {
        var participante = UsuariosDePrueba.NuevoParticipante();
        var dto = CrearEstrategia().Mapear(participante);

        dto.Id.Should().Be(participante.Id);
        dto.NombreUsuario.Should().Be(participante.NombreUsuario.Valor);
        dto.Correo.Should().Be(participante.Correo.Valor);
        dto.Rol.Should().Be("Participante");
        dto.Estado.Should().Be("Activo");
        dto.Nombre.Should().Be(participante.NombrePersona.Nombre);
        dto.Apellido.Should().Be(participante.NombrePersona.Apellido);
        dto.Sexo.Should().Be(participante.Sexo.ToString());
        dto.FechaNacimiento.Should().Be(participante.FechaNacimiento);
    }

    [Fact]
    public void Mapear_AsignaDatosContactoAnidados()
    {
        var participante = UsuariosDePrueba.NuevoParticipante();
        var dto = CrearEstrategia().Mapear(participante);

        dto.DatosContacto.Should().NotBeNull();
        dto.DatosContacto.Direccion.Should().Be(UsuariosDePrueba.Direccion);
        dto.DatosContacto.Telefono.Should().Be(UsuariosDePrueba.Telefono);
    }
}
