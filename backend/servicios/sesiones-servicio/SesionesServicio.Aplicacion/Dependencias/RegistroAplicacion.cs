using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Aplicacion.Comandos.CrearEquipo;
using SesionesServicio.Aplicacion.Comandos.CrearSesion;
using SesionesServicio.Aplicacion.Comandos.ModificarSesion;
using SesionesServicio.Aplicacion.Mapeadores;
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

        // Servicio de aplicación reutilizado por crear y modificar sesión para
        // validar las misiones contra juegos-servicio (existencia/activa/etapas).
        servicios.AddScoped<IValidadorMisionesSesion, ValidadorMisionesSesion>();

        // Factory Pattern para creación de sesiones. Los creadores son
        // stateless; agregar un nuevo tipo de sesión solo requiere registrar
        // un nuevo ICreadorSesion aquí, sin tocar la fábrica ni el manejador.
        servicios.AddSingleton<ICreadorSesion, CreadorSesionIndividual>();
        servicios.AddSingleton<ICreadorSesion, CreadorSesionGrupal>();
        servicios.AddSingleton<IFabricaSesion, FabricaSesion>();

        // Strategy Pattern para mapeo de DTOs por tipo de sesión. Agregar un
        // nuevo tipo solo requiere registrar sus estrategias; los manejadores
        // de consulta no cambian.
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
