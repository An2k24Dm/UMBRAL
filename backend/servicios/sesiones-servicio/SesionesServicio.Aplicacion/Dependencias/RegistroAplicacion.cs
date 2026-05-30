using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using SesionesServicio.Aplicacion.CasosDeUso.Comandos;
using SesionesServicio.Aplicacion.Validaciones;

namespace SesionesServicio.Aplicacion.Dependencias;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        var ensamblado = Assembly.GetExecutingAssembly();
        servicios.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(ensamblado));
        servicios.AddScoped<IValidador<CrearSesionComando>, ValidadorCrearSesion>();
        return servicios;
    }
}
