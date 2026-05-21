using System.Reflection;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace IdentidadServicio.Aplicacion.Dependencias;

public static class RegistroAplicacion
{
    public static IServiceCollection AgregarAplicacion(this IServiceCollection servicios)
    {
        var ensamblado = Assembly.GetExecutingAssembly();
        servicios.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(ensamblado));

        // Patrón Strategy: una implementación por cada tipo de usuario.
        servicios.AddScoped<IEstrategiaCreacionUsuario, EstrategiaCrearAdministrador>();
        servicios.AddScoped<IEstrategiaCreacionUsuario, EstrategiaCrearOperador>();
        servicios.AddScoped<IEstrategiaCreacionUsuario, EstrategiaCrearParticipante>();

        // Patrón Factory: recibe el IEnumerable<IEstrategiaCreacionUsuario>.
        servicios.AddScoped<FabricaEstrategiaCreacionUsuario>();

        // Strategy/Factory para el mapeo de perfil. Cada rol tiene su estrategia
        // y devuelve el DTO derivado correspondiente (HU06 / OCP).
        servicios.AddScoped<IEstrategiaMapeoPerfilUsuario, EstrategiaMapeoPerfilAdministrador>();
        servicios.AddScoped<IEstrategiaMapeoPerfilUsuario, EstrategiaMapeoPerfilOperador>();
        servicios.AddScoped<IEstrategiaMapeoPerfilUsuario, EstrategiaMapeoPerfilParticipante>();
        servicios.AddScoped<FabricaEstrategiaMapeoPerfilUsuario>();

        // Validadores reutilizables expuestos como IValidador<TDatos>. Cada
        // caso de uso recibe la especialización con tipado fuerte del DTO.
        // HU02 — registro de Operador/Administrador desde el panel web.
        servicios.AddScoped<IValidador<CrearUsuarioDto>, ValidadorCrearUsuario>();
        // HU03 — registro público de Participante desde la app móvil.
        servicios.AddScoped<IValidador<RegistrarParticipanteDto>, ValidadorRegistrarParticipante>();

        // Generador de códigos correlativos (HU02): OP-### / AD-###.
        servicios.AddScoped<IGeneradorCodigoUsuario, GeneradorCodigoUsuario>();

        return servicios;
    }
}
