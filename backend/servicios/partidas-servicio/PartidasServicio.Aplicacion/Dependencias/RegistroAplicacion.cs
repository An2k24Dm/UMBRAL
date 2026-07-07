using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PartidasServicio.Aplicacion.Cadena;
using PartidasServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;
using PartidasServicio.Aplicacion.Estrategias;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Aplicacion.Servicios;
using PartidasServicio.Aplicacion.Validaciones;

namespace PartidasServicio.Aplicacion.Dependencias;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        servicios.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

        // Validadores
        servicios.AddScoped<IValidador<EnviarRespuestaTriviaComando>,
            ValidadorEnviarRespuestaTrivia>();

        // Cadena de responsabilidad
        servicios.AddScoped<EslabonEstadoPartida>();
        servicios.AddScoped<EslabonEstadoSesion>();
        servicios.AddScoped<EslabonParticipanteEnSesion>();
        servicios.AddScoped<EslabonConcurrencia>();

        // Strategy
        servicios.AddSingleton<ICalculadoraPuntaje, CalculadoraPuntajePorTiempo>();

        // Facade
        servicios.AddScoped<IServicioPartidas, ServicioPartidas>();

        return servicios;
    }
}
