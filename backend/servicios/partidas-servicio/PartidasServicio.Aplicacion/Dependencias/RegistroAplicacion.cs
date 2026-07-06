using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PartidasServicio.Aplicacion.Cadena;
using PartidasServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;
using PartidasServicio.Aplicacion.Estrategias;
using PartidasServicio.Aplicacion.Puertos;
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
        servicios.AddScoped<EslabonEstadoSesion>();
        servicios.AddScoped<EslabonParticipanteEnSesion>();
        servicios.AddScoped<EslabonConcurrencia>();

        // Strategy
        servicios.AddSingleton<ICalculadoraPuntaje, CalculadoraPuntajePorTiempo>();

        return servicios;
    }
}
