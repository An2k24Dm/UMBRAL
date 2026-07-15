using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Aplicacion.Servicios;
using SesionesServicio.Aplicacion.Comandos.AbandonarSesion;
using SesionesServicio.Aplicacion.Comandos.CrearEquipo;
using SesionesServicio.Aplicacion.Comandos.CrearSesion;
using SesionesServicio.Aplicacion.Comandos.ExpulsarParticipanteEquipo;
using SesionesServicio.Aplicacion.Comandos.IngresarEquipo;
using SesionesServicio.Aplicacion.Comandos.IngresarSesionPorCodigo;
using SesionesServicio.Aplicacion.Comandos.ModificarEquipo;
using SesionesServicio.Aplicacion.Comandos.ModificarSesion;
using SesionesServicio.Aplicacion.Cadena.EvidenciasTesoro;
using SesionesServicio.Aplicacion.Fachadas;
using SesionesServicio.Aplicacion.Mapeadores;
using SesionesServicio.Aplicacion.Mapeadores.IngresoSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Aplicacion.Validaciones.OperacionSesion;
using SesionesServicio.Dominio.Fabricas;

namespace SesionesServicio.Aplicacion.Dependencias;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        var ensamblado = Assembly.GetExecutingAssembly();
        servicios.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(ensamblado));
        servicios.AddScoped<IValidador<AbandonarSesionComando>, ValidadorAbandonarSesion>();
        servicios.AddScoped<IValidador<CrearSesionComando>, ValidadorCrearSesion>();
        servicios.AddScoped<IValidador<ModificarSesionComando>, ValidadorModificarSesion>();
        servicios.AddScoped<IValidador<CrearEquipoComando>, ValidadorCrearEquipo>();
        servicios.AddScoped<IValidador<IngresarEquipoComando>, IngresarEquipoValidador>();
        servicios.AddScoped<IValidador<ExpulsarParticipanteEquipoComando>,
            ValidadorExpulsarParticipanteEquipo>();
        servicios.AddScoped<IValidador<IngresarSesionPorCodigoComando>,
            ValidadorIngresarSesionPorCodigo>();
        servicios.AddScoped<IValidador<ModificarEquipoComando>, ValidadorModificarEquipo>();
        servicios.AddScoped<IFachadaOperacionSesion, FachadaOperacionSesion>();
        servicios.AddScoped<ValidadorInicioSesionOperacion>();
        servicios.AddScoped<ValidadorCancelacionSesionOperacion>();
        servicios.AddScoped<ValidadorAccionJuegoSesion>();
        servicios.AddScoped<Autorizacion.PoliticaParticipacionUnicaSesion>();
        servicios.AddScoped<ConstructorRespuestaIngresoSesion>();
        servicios.AddScoped<IValidadorMisionesSesion, ValidadorMisionesSesion>();
        servicios.AddSingleton<ICreadorSesion, CreadorSesionIndividual>();
        servicios.AddSingleton<ICreadorSesion, CreadorSesionGrupal>();
        servicios.AddSingleton<IFabricaSesion, FabricaSesion>();
        servicios.AddSingleton<IMapeadorDetalleSesion, MapeadorDetalleSesionIndividual>();
        servicios.AddSingleton<IMapeadorDetalleSesion, MapeadorDetalleSesionGrupal>();
        servicios.AddSingleton<FabricaMapeadorDetalleSesion>();

        servicios.AddSingleton<IMapeadorListadoSesion, MapeadorListadoSesionIndividual>();
        servicios.AddSingleton<IMapeadorListadoSesion, MapeadorListadoSesionGrupal>();
        servicios.AddSingleton<FabricaMapeadorListadoSesion>();

        servicios.AddSingleton<IMapeadorSesionDisponibleMovil, MapeadorSesionDisponibleMovilIndividual>();
        servicios.AddSingleton<IMapeadorSesionDisponibleMovil, MapeadorSesionDisponibleMovilGrupal>();
        servicios.AddSingleton<FabricaMapeadorSesionDisponibleMovil>();

        servicios.AddScoped<IServicioFinalizacionSesion, ServicioFinalizacionSesion>();
        servicios.AddScoped<IServicioProgresoSecuencialSesion, ServicioProgresoSecuencialSesion>();

        // Chain of Responsibility para la validación estructural de evidencias de
        // Búsqueda del Tesoro. Scoped: los eslabones dependen de repositorios,
        // cliente HTTP y servicios de ciclo de vida por petición. La fábrica
        // enlaza los eslabones y expone el primero de la cadena.
        servicios.AddScoped<EslabonSesionActiva>();
        servicios.AddScoped<EslabonParticipanteInscrito>();
        servicios.AddScoped<EslabonEtapaActual>();
        servicios.AddScoped<EslabonEvidenciaNoDuplicada>();
        servicios.AddScoped<EslabonCodigoQr>();
        servicios.AddScoped<FabricaCadenaValidacionEvidenciaTesoro>();
        // Reloj de Trivia por jugador con la espera de feedback autoritativa (5 s
        // por defecto). Fuente única del intervalo entre preguntas.
        servicios.AddSingleton<IServicioTiempoTriviaSesion>(
            _ => new ServicioTiempoTriviaSesion());

        // Procesos automáticos disparados por BackgroundServices de Infraestructura
        // a través de puertos (la lógica vive aquí, en Aplicación).
        servicios.AddScoped<IProcesadorPreparacionSesiones,
            Procesos.PreparacionSesiones.ProcesadorPreparacionSesiones>();
        servicios.AddScoped<IProcesadorVencimientosEtapas,
            Procesos.VencimientoEtapas.ProcesadorVencimientoEtapasSesion>();

        return servicios;
    }
}
