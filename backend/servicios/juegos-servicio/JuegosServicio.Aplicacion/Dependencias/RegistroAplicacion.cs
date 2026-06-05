using System.Reflection;
using JuegosServicio.Aplicacion.CasosDeUso.Comandos;
using JuegosServicio.Aplicacion.Validaciones;
using Microsoft.Extensions.DependencyInjection;

namespace JuegosServicio.Aplicacion.Dependencias;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        servicios.AddScoped<IValidador<CrearTriviaComando>, ValidadorCrearTrivia>();
        servicios.AddScoped<IValidador<ModificarTriviaComando>, ValidadorModificarTrivia>();
        servicios.AddScoped<IValidador<AgregarPreguntaComando>, ValidadorAgregarPregunta>();
        servicios.AddScoped<IValidador<CrearBusquedaTesoroComando>, ValidadorCrearBusquedaTesoro>();
        servicios.AddScoped<IValidador<ModificarBusquedaTesoroComando>, ValidadorModificarBusquedaTesoro>();
        servicios.AddScoped<IValidador<AgregarPistaComando>, ValidadorAgregarPista>();
        servicios.AddScoped<IValidador<CrearMisionComando>, ValidadorCrearMision>();
        servicios.AddScoped<IValidador<ModificarMisionComando>, ValidadorModificarMision>();
        servicios.AddScoped<IValidador<AgregarEtapaComando>, ValidadorAgregarEtapa>();

        return servicios;
    }
}
