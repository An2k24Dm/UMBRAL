using FluentAssertions;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using Moq;

namespace IdentidadServicio.PruebasUnitarias.Estrategias;

public class FabricaEstrategiaCreacionUsuarioPruebas
{
    private static FabricaEstrategiaCreacionUsuario CrearFabrica()
    {
        var generador = new Mock<IGeneradorCodigoUsuario>().Object;
        return new FabricaEstrategiaCreacionUsuario(new IEstrategiaCreacionUsuario[]
        {
            new EstrategiaCrearAdministrador(generador),
            new EstrategiaCrearOperador(generador),
            new EstrategiaCrearParticipante()
        });
    }

    [Fact]
    public void Obtener_Administrador_DevuelveEstrategiaCrearAdministrador()
    {
        var estrategia = CrearFabrica().Obtener(RolUsuario.Administrador);
        estrategia.Should().BeOfType<EstrategiaCrearAdministrador>();
        estrategia.ObtenerRol().Should().Be(RolUsuario.Administrador);
    }

    [Fact]
    public void Obtener_Operador_DevuelveEstrategiaCrearOperador()
    {
        var estrategia = CrearFabrica().Obtener(RolUsuario.Operador);
        estrategia.Should().BeOfType<EstrategiaCrearOperador>();
        estrategia.ObtenerRol().Should().Be(RolUsuario.Operador);
    }

    [Fact]
    public void Obtener_Participante_DevuelveEstrategiaCrearParticipante()
    {
        var estrategia = CrearFabrica().Obtener(RolUsuario.Participante);
        estrategia.Should().BeOfType<EstrategiaCrearParticipante>();
        estrategia.ObtenerRol().Should().Be(RolUsuario.Participante);
    }

    [Fact]
    public void Obtener_TipoInvalido_LanzaRolNoValido()
    {
        Action accion = () => CrearFabrica().Obtener((RolUsuario)999);
        accion.Should().Throw<RolNoValidoExcepcion>();
    }
}
