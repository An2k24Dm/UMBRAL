using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Aplicacion.Comandos.CrearEquipo;
using SesionesServicio.Aplicacion.Comandos.CrearSesion;
using SesionesServicio.Aplicacion.Comandos.IngresarEquipo;
using SesionesServicio.Aplicacion.Comandos.IngresarSesionPorCodigo;
using SesionesServicio.Aplicacion.Comandos.ModificarEquipo;
using SesionesServicio.Aplicacion.Comandos.ModificarSesion;
using SesionesServicio.Aplicacion.Mapeadores;
using SesionesServicio.Aplicacion.Mapeadores.IngresoSesion;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Fabricas;

namespace SesionesServicio.Aplicacion.Dependencias;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        var ensamblado = Assembly.GetExecutingAssembly();
        servicios.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(ensamblado));
        servicios.AddScoped<IValidador<CrearSesionComando>, ValidadorCrearSesion>();
        servicios.AddScoped<IValidador<ModificarSesionComando>, ValidadorModificarSesion>();
        servicios.AddScoped<IValidador<CrearEquipoComando>, ValidadorCrearEquipo>();
        servicios.AddScoped<IValidador<IngresarEquipoComando>, IngresarEquipoValidador>();
        servicios.AddScoped<IValidador<IngresarSesionPorCodigoComando>,
            ValidadorIngresarSesionPorCodigo>();
        servicios.AddScoped<IValidador<ModificarEquipoComando>, ValidadorModificarEquipo>();
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

        return servicios;
    }
}
