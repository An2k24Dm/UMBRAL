using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.ResetearContrasenaUsuario;

public sealed class ResetearContrasenaUsuarioManejador
    : IRequestHandler<ResetearContrasenaUsuarioComando, ResetearContrasenaRespuestaDto>
{
    private readonly IRepositorioUsuariosLectura _lectura;
    private readonly IRepositorioControlContrasenaTemporal _controlContrasena;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IGeneradorContrasenaTemporal _generador;
    private readonly IServicioCorreo _correo;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ResetearContrasenaUsuarioManejador(
        IRepositorioUsuariosLectura lectura,
        IRepositorioControlContrasenaTemporal controlContrasena,
        IProveedorIdentidad proveedor,
        IGeneradorContrasenaTemporal generador,
        IServicioCorreo correo,
        IRegistroLogsAplicacion registroLogs)
    {
        _lectura = lectura;
        _controlContrasena = controlContrasena;
        _proveedor = proveedor;
        _generador = generador;
        _correo = correo;
        _registroLogs = registroLogs;
    }

    public async Task<ResetearContrasenaRespuestaDto> Handle(
        ResetearContrasenaUsuarioComando comando, CancellationToken cancelacion)
    {
        var usuario = await _lectura.ObtenerUsuarioInternoPorIdAsync(comando.IdUsuario, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un usuario interno con id {comando.IdUsuario}.");

        if (usuario.Rol != RolUsuario.Operador && usuario.Rol != RolUsuario.Administrador)
            throw new DatosUsuarioInvalidosExcepcion(
                "El reseteo de contraseña solo aplica a Operador o Administrador.");

        var idKeycloak = await _lectura.ObtenerIdKeycloakUsuarioInternoAsync(
            comando.IdUsuario, cancelacion);
        if (string.IsNullOrWhiteSpace(idKeycloak))
        {
            _registroLogs.Advertencia(
                evento: "UsuarioSinIdKeycloak",
                descripcion: "Reset de contraseña abortado: el usuario no tiene IdKeycloak asociado.",
                propiedades: new Dictionary<string, object?>
                {
                    ["UsuarioId"] = usuario.Id,
                    ["NombreUsuario"] = usuario.NombreUsuario.Valor,
                    ["Rol"] = usuario.Rol.ToString()
                });
            throw new InvalidOperationException(
                $"El usuario {comando.IdUsuario} no tiene IdKeycloak asociado: " +
                "imposible resetear contraseña en Keycloak.");
        }

        _registroLogs.Informacion(
            evento: "ResetContrasenaIniciado",
            descripcion: "Iniciando reseteo de contraseña.",
            propiedades: new Dictionary<string, object?>
            {
                ["NombreUsuario"] = usuario.NombreUsuario.Valor,
                ["UsuarioId"] = usuario.Id,
                ["IdKeycloak"] = idKeycloak,
                ["Rol"] = usuario.Rol.ToString()
            });

        var contrasenaTemporal = _generador.Generar();

        try
        {
            await _proveedor.CambiarContrasenaAsync(
                idKeycloak, contrasenaTemporal, cancelacion);
        }
        catch (Exception ex)
        {
            _registroLogs.Error(
                excepcion: ex,
                evento: "ResetContrasenaRechazado",
                descripcion: "Keycloak rechazó el reset de contraseña. No se envía correo.",
                propiedades: new Dictionary<string, object?>
                {
                    ["NombreUsuario"] = usuario.NombreUsuario.Valor,
                    ["UsuarioId"] = usuario.Id,
                    ["IdKeycloak"] = idKeycloak
                });
            throw;
        }

        await _controlContrasena.MarcarDebeCambiarPorIdAsync(usuario.Id, cancelacion);

        var cuerpo = MensajesContrasenaTemporal.CuerpoReseteo(
            nombreCompleto: $"{usuario.NombrePersona.Nombre} {usuario.NombrePersona.Apellido}".Trim(),
            nombreUsuario: usuario.NombreUsuario.Valor,
            correoAcceso: usuario.Correo.Valor,
            contrasenaTemporal: contrasenaTemporal);

        await _correo.EnviarAsync(
            usuario.Correo.Valor,
            MensajesContrasenaTemporal.AsuntoReseteo,
            cuerpo,
            cancelacion);

        _registroLogs.Informacion(
            evento: "ContrasenaUsuarioReseteada",
            descripcion: "Administrador reseteó la contraseña temporal de un usuario correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["NombreUsuario"] = usuario.NombreUsuario.Valor,
                ["UsuarioId"] = usuario.Id,
                ["IdKeycloak"] = idKeycloak,
                ["Correo"] = usuario.Correo.Valor
            });

        return new ResetearContrasenaRespuestaDto
        {
            IdUsuario = usuario.Id,
            CorreoDestino = usuario.Correo.Valor,
            Mensaje = "Se envió una contraseña temporal al correo del usuario."
        };
    }
}
