using FluentAssertions;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.PruebasUnitarias.Estrategias;

public class FabricaEstrategiaCreacionUsuarioPruebas
{
    private static FabricaEstrategiaCreacionUsuario CrearFabrica() => new(new IEstrategiaCreacionUsuario[]
    {
        new EstrategiaCrearAdministrador(),
        new EstrategiaCrearOperador(),
        new EstrategiaCrearParticipante()
    });

    [Fact]
    public void Obtener_Administrador_DevuelveEstrategiaCrearAdministrador()
    {
        var estrategia = CrearFabrica().Obtener(TipoUsuario.Administrador);
        estrategia.Should().BeOfType<EstrategiaCrearAdministrador>();
        estrategia.ObtenerRol().Should().Be(RolUsuario.Administrador);
    }

    [Fact]
    public void Obtener_Operador_DevuelveEstrategiaCrearOperador()
    {
        var estrategia = CrearFabrica().Obtener(TipoUsuario.Operador);
        estrategia.Should().BeOfType<EstrategiaCrearOperador>();
        estrategia.ObtenerRol().Should().Be(RolUsuario.Operador);
    }

    [Fact]
    public void Obtener_Participante_DevuelveEstrategiaCrearParticipante()
    {
        var estrategia = CrearFabrica().Obtener(TipoUsuario.Participante);
        estrategia.Should().BeOfType<EstrategiaCrearParticipante>();
        estrategia.ObtenerRol().Should().Be(RolUsuario.Participante);
    }

    [Fact]
    public void Obtener_TipoInvalido_LanzaRolNoValido()
    {
        Action accion = () => CrearFabrica().Obtener((TipoUsuario)999);
        accion.Should().Throw<RolNoValidoExcepcion>();
    }
}
