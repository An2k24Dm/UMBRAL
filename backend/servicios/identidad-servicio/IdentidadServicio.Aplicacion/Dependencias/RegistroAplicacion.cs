using System.Reflection;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Aplicacion.Validaciones;
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

        // Reglas comunes de validación de usuario reutilizadas por los
        // validadores específicos (HU02 / HU03 / HU09). Centraliza Regex,
        // mensajes, longitudes y la edad mínima/máxima en un único lugar.
        servicios.AddScoped<IReglasValidacionUsuario, ReglasValidacionUsuario>();

        // Validadores tipados por comando. Cada caso de uso recibe su propio
        // IValidador<TComando> con tipado fuerte. La detección de duplicados
        // (que requiere repositorio) vive en el manejador, no aquí.
        // HU02 — registro de Operador/Administrador desde el panel web.
        servicios.AddScoped<IValidador<CrearUsuarioComando>, ValidadorCrearUsuario>();
        // HU03 — registro público de Participante desde la app móvil.
        servicios.AddScoped<IValidador<RegistrarParticipanteComando>, ValidadorRegistrarParticipante>();
        // HU09 — edición parcial de Operador desde el panel web.
        servicios.AddScoped<IValidador<ModificarOperadorComando>, ValidadorModificarOperador>();
        // HU09 — validador asíncrono de unicidad (duplicados que excluyen al
        // propio Operador). Vive separado porque requiere repositorio.
        servicios.AddScoped<
            IValidadorAsincrono<ModificarOperadorComando>,
            ValidadorUnicidadModificarOperador>();
        // HU10 — edición del propio perfil del Participante desde la app móvil.
        servicios.AddScoped<
            IValidador<ModificarParticipanteComando>,
            ValidadorModificarParticipante>();
        servicios.AddScoped<
            IValidadorAsincrono<ModificarParticipanteComando>,
            ValidadorUnicidadModificarParticipante>();
        // Servicio puro de aplicación que detecta y aplica los cambios
        // reales sobre el agregado Usuario (sin EF, sin Keycloak). Reusado
        // por HU09 (Operador) y HU10 (Participante).
        servicios.AddSingleton<AplicadorCambiosUsuario>();

        // Generador de códigos correlativos (HU02): OP-### / AD-###.
        servicios.AddScoped<IGeneradorCodigoUsuario, GeneradorCodigoUsuario>();

        return servicios;
    }
}
