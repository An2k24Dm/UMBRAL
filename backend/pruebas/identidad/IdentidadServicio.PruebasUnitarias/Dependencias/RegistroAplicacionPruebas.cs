using IdentidadServicio.Aplicacion.Comandos.CambiarContrasenaObligatoria;
using IdentidadServicio.Aplicacion.Comandos.CrearUsuario;
using IdentidadServicio.Aplicacion.Comandos.ModificarOperador;
using IdentidadServicio.Aplicacion.Comandos.ModificarParticipante;
using IdentidadServicio.Aplicacion.Comandos.RegistrarParticipante;
using IdentidadServicio.Aplicacion.Dependencias;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Aplicacion.Validaciones;
using Microsoft.Extensions.DependencyInjection;

namespace IdentidadServicio.PruebasUnitarias.Dependencias;

public class RegistroAplicacionPruebas
{
    [Fact]
    public void AgregarAplicacion_RegistraServiciosDeAplicacionEsperados()
    {
        var servicios = new ServiceCollection();

        servicios.AgregarAplicacion();

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IEstrategiaCreacionUsuario) &&
            d.ImplementationType == typeof(EstrategiaCrearAdministrador) &&
            d.Lifetime == ServiceLifetime.Scoped);
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IEstrategiaCreacionUsuario) &&
            d.ImplementationType == typeof(EstrategiaCrearOperador));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IEstrategiaCreacionUsuario) &&
            d.ImplementationType == typeof(EstrategiaCrearParticipante));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(FabricaEstrategiaCreacionUsuario));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IEstrategiaMapeoPerfilUsuario) &&
            d.ImplementationType == typeof(EstrategiaMapeoPerfilAdministrador));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IEstrategiaMapeoPerfilUsuario) &&
            d.ImplementationType == typeof(EstrategiaMapeoPerfilOperador));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IEstrategiaMapeoPerfilUsuario) &&
            d.ImplementationType == typeof(EstrategiaMapeoPerfilParticipante));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(FabricaEstrategiaMapeoPerfilUsuario));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(AplicadorCambiosUsuario) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AgregarAplicacion_RegistraValidadoresGeneradoresYAutorizadores()
    {
        var servicios = new ServiceCollection();

        servicios.AgregarAplicacion();

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IReglasValidacionUsuario) &&
            d.ImplementationType == typeof(ReglasValidacionUsuario));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<CrearUsuarioComando>) &&
            d.ImplementationType == typeof(ValidadorCrearUsuario));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<CambiarContrasenaObligatoriaComando>) &&
            d.ImplementationType == typeof(ValidadorCambiarContrasenaObligatoria));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<RegistrarParticipanteComando>) &&
            d.ImplementationType == typeof(ValidadorRegistrarParticipante));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<ModificarOperadorComando>) &&
            d.ImplementationType == typeof(ValidadorModificarOperador));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidadorAsincrono<ModificarOperadorComando>) &&
            d.ImplementationType == typeof(ValidadorUnicidadModificarOperador));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<ModificarParticipanteComando>) &&
            d.ImplementationType == typeof(ValidadorModificarParticipante));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidadorAsincrono<ModificarParticipanteComando>) &&
            d.ImplementationType == typeof(ValidadorUnicidadModificarParticipante));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IGeneradorCodigoUsuario) &&
            d.ImplementationType == typeof(GeneradorCodigoUsuario));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IGeneradorContrasenaTemporal) &&
            d.ImplementationType == typeof(GeneradorContrasenaTemporal) &&
            d.Lifetime == ServiceLifetime.Singleton);
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IAutorizadorUsuarioActivo) &&
            d.ImplementationType == typeof(AutorizadorUsuarioActivo));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidadorAccesoUsuarioActivo) &&
            d.ImplementationType == typeof(ValidadorAccesoUsuarioActivo));
    }
}
