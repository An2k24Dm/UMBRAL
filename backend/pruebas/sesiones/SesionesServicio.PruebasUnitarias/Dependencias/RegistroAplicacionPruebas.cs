using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Aplicacion.Autorizacion;
using SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;
using SesionesServicio.Aplicacion.Comandos.AbandonarSesion;
using SesionesServicio.Aplicacion.Comandos.CrearEquipo;
using SesionesServicio.Aplicacion.Comandos.CrearSesion;
using SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteEquipo;
using SesionesServicio.Aplicacion.Comandos.IngresarEquipo;
using SesionesServicio.Aplicacion.Comandos.IngresarSesionPorCodigo;
using SesionesServicio.Aplicacion.Comandos.ModificarEquipo;
using SesionesServicio.Aplicacion.Comandos.ModificarSesion;
using SesionesServicio.Aplicacion.Dependencias;
using SesionesServicio.Aplicacion.Fachadas;
using SesionesServicio.Aplicacion.Mapeadores;
using SesionesServicio.Aplicacion.Mapeadores.IngresoSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Servicios;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Aplicacion.Validaciones.OperacionSesion;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Fabricas;

namespace SesionesServicio.PruebasUnitarias.Dependencias;

public class RegistroAplicacionPruebas
{
    [Fact]
    public void AgregarAplicacion_RegistraValidadoresYServiciosDeOperacion()
    {
        var servicios = new ServiceCollection();

        servicios.AgregarAplicacion();

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<AbandonarSesionComando>) &&
            d.ImplementationType == typeof(ValidadorAbandonarSesion));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<CrearSesionComando>) &&
            d.ImplementationType == typeof(ValidadorCrearSesion));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<ModificarSesionComando>) &&
            d.ImplementationType == typeof(ValidadorModificarSesion));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<CrearEquipoComando>) &&
            d.ImplementationType == typeof(ValidadorCrearEquipo));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<IngresarEquipoComando>) &&
            d.ImplementationType == typeof(IngresarEquipoValidador));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<ExpulsarParticipanteEquipoComando>) &&
            d.ImplementationType == typeof(ValidadorExpulsarParticipanteEquipo));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<IngresarSesionPorCodigoComando>) &&
            d.ImplementationType == typeof(ValidadorIngresarSesionPorCodigo));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidador<ModificarEquipoComando>) &&
            d.ImplementationType == typeof(ValidadorModificarEquipo));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IFachadaOperacionSesion) &&
            d.ImplementationType == typeof(FachadaOperacionSesion));
        servicios.Should().Contain(d => d.ServiceType == typeof(ValidadorInicioSesionOperacion));
        servicios.Should().Contain(d => d.ServiceType == typeof(ValidadorCancelacionSesionOperacion));
        servicios.Should().Contain(d => d.ServiceType == typeof(ValidadorAccionJuegoSesion));
        servicios.Should().Contain(d => d.ServiceType == typeof(PoliticaParticipacionUnicaSesion));
        servicios.Should().Contain(d => d.ServiceType == typeof(ConstructorRespuestaIngresoSesion));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IValidadorMisionesSesion) &&
            d.ImplementationType == typeof(ValidadorMisionesSesion));
    }

    [Fact]
    public void AgregarAplicacion_RegistraFabricasMapeadoresProcesosYCadenaTesoro()
    {
        var servicios = new ServiceCollection();

        servicios.AgregarAplicacion();

        servicios.Should().Contain(d =>
            d.ServiceType == typeof(ICreadorSesion) &&
            d.ImplementationType == typeof(CreadorSesionIndividual) &&
            d.Lifetime == ServiceLifetime.Singleton);
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(ICreadorSesion) &&
            d.ImplementationType == typeof(CreadorSesionGrupal));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IFabricaSesion) &&
            d.ImplementationType == typeof(FabricaSesion));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IMapeadorDetalleSesion) &&
            d.ImplementationType == typeof(MapeadorDetalleSesionIndividual));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IMapeadorDetalleSesion) &&
            d.ImplementationType == typeof(MapeadorDetalleSesionGrupal));
        servicios.Should().Contain(d => d.ServiceType == typeof(FabricaMapeadorDetalleSesion));
        servicios.Should().Contain(d => d.ServiceType == typeof(FabricaMapeadorListadoSesion));
        servicios.Should().Contain(d => d.ServiceType == typeof(FabricaMapeadorSesionDisponibleMovil));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IServicioFinalizacionSesion) &&
            d.ImplementationType == typeof(ServicioFinalizacionSesion));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IServicioProgresoSecuencialSesion) &&
            d.ImplementationType == typeof(ServicioProgresoSecuencialSesion));
        servicios.Should().Contain(d => d.ServiceType == typeof(EslabonSesionActiva));
        servicios.Should().Contain(d => d.ServiceType == typeof(EslabonParticipanteInscrito));
        servicios.Should().Contain(d => d.ServiceType == typeof(EslabonEtapaActual));
        servicios.Should().Contain(d => d.ServiceType == typeof(EslabonEvidenciaNoDuplicada));
        servicios.Should().Contain(d => d.ServiceType == typeof(EslabonCodigoQr));
        servicios.Should().Contain(d => d.ServiceType == typeof(FabricaCadenaValidacionEvidenciaTesoro));
        servicios.Should().Contain(d =>
            d.ServiceType == typeof(IServicioTiempoTriviaSesion) &&
            d.Lifetime == ServiceLifetime.Singleton);
        servicios.Should().Contain(d => d.ServiceType == typeof(IProcesadorPreparacionSesiones));
        servicios.Should().Contain(d => d.ServiceType == typeof(IProcesadorVencimientosEtapas));
    }
}
