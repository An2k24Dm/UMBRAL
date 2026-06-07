using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ResetearContrasenaUsuarioManejador
    : IRequestHandler<ResetearContrasenaUsuarioComando, ResetearContrasenaRespuestaDto>
{
    private readonly IRepositorioUsuariosLectura _lectura;
    private readonly IRepositorioControlContrasenaTemporal _controlContrasena;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IGeneradorContrasenaTemporal _generador;
    private readonly IServicioCorreo _correo;
    private readonly ILogger<ResetearContrasenaUsuarioManejador> _registro;

    public ResetearContrasenaUsuarioManejador(
        IRepositorioUsuariosLectura lectura,
        IRepositorioControlContrasenaTemporal controlContrasena,
        IProveedorIdentidad proveedor,
        IGeneradorContrasenaTemporal generador,
        IServicioCorreo correo,
        ILogger<ResetearContrasenaUsuarioManejador> registro)
    {
        _lectura = lectura;
        _controlContrasena = controlContrasena;
        _proveedor = proveedor;
        _generador = generador;
        _correo = correo;
        _registro = registro;
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
            _registro.LogError(
                "Reset de contraseña abortado: usuario {Id} ({NombreUsuario}, rol={Rol}) " +
                "no tiene IdKeycloak asociado.",
                usuario.Id, usuario.NombreUsuario.Valor, usuario.Rol);
            throw new InvalidOperationException(
                $"El usuario {comando.IdUsuario} no tiene IdKeycloak asociado: " +
                "imposible resetear contraseña en Keycloak.");
        }

        _registro.LogInformation(
            "Iniciando reseteo de contraseña. Usuario={NombreUsuario}, Id={Id}, " +
            "IdKeycloak={IdKeycloak}, Rol={Rol}.",
            usuario.NombreUsuario.Valor, usuario.Id, idKeycloak, usuario.Rol);

        var contrasenaTemporal = _generador.Generar();

        try
        {
            await _proveedor.CambiarContrasenaAsync(
                idKeycloak, contrasenaTemporal, cancelacion);
        }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "Keycloak rechazó el reset de contraseña. Usuario={NombreUsuario}, " +
                "Id={Id}, IdKeycloak={IdKeycloak}. No se envía correo.",
                usuario.NombreUsuario.Valor, usuario.Id, idKeycloak);
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

        _registro.LogInformation(
            "Reseteo de contraseña completado. Usuario={NombreUsuario}, Id={Id}, " +
            "IdKeycloak={IdKeycloak}, correo={Correo}.",
            usuario.NombreUsuario.Valor, usuario.Id, idKeycloak, usuario.Correo.Valor);

        return new ResetearContrasenaRespuestaDto
        {
            IdUsuario = usuario.Id,
            CorreoDestino = usuario.Correo.Valor,
            Mensaje = "Se envió una contraseña temporal al correo del usuario."
        };
    }
}
