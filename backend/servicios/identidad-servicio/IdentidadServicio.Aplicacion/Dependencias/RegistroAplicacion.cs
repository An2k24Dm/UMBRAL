using System.Reflection;
using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Aplicacion.Puertos;
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
        servicios.AddScoped<IEstrategiaCreacionUsuario, EstrategiaCrearAdministrador>();
        servicios.AddScoped<IEstrategiaCreacionUsuario, EstrategiaCrearOperador>();
        servicios.AddScoped<IEstrategiaCreacionUsuario, EstrategiaCrearParticipante>();
        servicios.AddScoped<FabricaEstrategiaCreacionUsuario>();
        servicios.AddScoped<IEstrategiaMapeoPerfilUsuario, EstrategiaMapeoPerfilAdministrador>();
        servicios.AddScoped<IEstrategiaMapeoPerfilUsuario, EstrategiaMapeoPerfilOperador>();
        servicios.AddScoped<IEstrategiaMapeoPerfilUsuario, EstrategiaMapeoPerfilParticipante>();
        servicios.AddScoped<FabricaEstrategiaMapeoPerfilUsuario>();
        servicios.AddScoped<IReglasValidacionUsuario, ReglasValidacionUsuario>();
        servicios.AddScoped<IValidador<CrearUsuarioComando>, ValidadorCrearUsuario>();
        servicios.AddScoped<
            IValidador<CambiarContrasenaObligatoriaComando>,
            ValidadorCambiarContrasenaObligatoria>();
        servicios.AddScoped<IValidador<RegistrarParticipanteComando>, ValidadorRegistrarParticipante>();
        // Validación de unicidad reutilizable (HU02 creación administrativa y
        // HU03 registro de participante). Una sola clase concreta con métodos
        // por caso de uso; no se expone como IValidadorAsincrono<T>.
        servicios.AddScoped<ValidadorUnicidadUsuario>();
        servicios.AddScoped<IValidador<ModificarOperadorComando>, ValidadorModificarOperador>();
        servicios.AddScoped<
            IValidadorAsincrono<ModificarOperadorComando>,
            ValidadorUnicidadModificarOperador>();
        servicios.AddScoped<
            IValidador<ModificarParticipanteComando>,
            ValidadorModificarParticipante>();
        servicios.AddScoped<
            IValidadorAsincrono<ModificarParticipanteComando>,
            ValidadorUnicidadModificarParticipante>();
        servicios.AddSingleton<AplicadorCambiosUsuario>();
        servicios.AddScoped<IGeneradorCodigoUsuario, GeneradorCodigoUsuario>();
        servicios.AddSingleton<IGeneradorContrasenaTemporal, GeneradorContrasenaTemporal>();
        servicios.AddScoped<IAutorizadorUsuarioActivo, AutorizadorUsuarioActivo>();
        servicios.AddScoped<IValidadorAccesoUsuarioActivo, ValidadorAccesoUsuarioActivo>();

        return servicios;
    }
}
