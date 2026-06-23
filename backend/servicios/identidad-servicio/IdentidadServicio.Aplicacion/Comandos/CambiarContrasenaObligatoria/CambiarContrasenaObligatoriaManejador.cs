using IdentidadServicio.Aplicacion.Mapeadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.Comandos.CambiarContrasenaObligatoria;

public sealed class CambiarContrasenaObligatoriaManejador
    : IRequestHandler<CambiarContrasenaObligatoriaComando, CambiarContrasenaObligatoriaRespuestaDto>
{
    private readonly IRepositorioUsuariosLectura _lectura;
    private readonly IRepositorioControlContrasenaTemporal _control;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IValidador<CambiarContrasenaObligatoriaComando> _validador;
    private readonly ILogger<CambiarContrasenaObligatoriaManejador> _registro;

    public CambiarContrasenaObligatoriaManejador(
        IRepositorioUsuariosLectura lectura,
        IRepositorioControlContrasenaTemporal control,
        IProveedorIdentidad proveedor,
        IValidador<CambiarContrasenaObligatoriaComando> validador,
        ILogger<CambiarContrasenaObligatoriaManejador> registro)
    {
        _lectura = lectura;
        _control = control;
        _proveedor = proveedor;
        _validador = validador;
        _registro = registro;
    }

    public async Task<CambiarContrasenaObligatoriaRespuestaDto> Handle(
        CambiarContrasenaObligatoriaComando comando, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(comando.IdKeycloak))
            throw new DatosUsuarioInvalidosExcepcion(
                "No se pudo determinar la identidad del usuario autenticado.");

        _validador.Validar(comando).LanzarSiHayErrores();

        var usuario = await _lectura.ObtenerPorIdKeycloakAsync(comando.IdKeycloak, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                "El usuario no está registrado en UMBRAL.");

        if (usuario.Rol != RolUsuario.Operador && usuario.Rol != RolUsuario.Administrador)
            throw new AccesoNoPermitidoExcepcion(
                "El cambio obligatorio de contraseña solo aplica a Operador o Administrador.");

        var debeCambiar = await _control.ObtenerDebeCambiarPorIdKeycloakAsync(
            comando.IdKeycloak, cancelacion);
        if (!debeCambiar)
            throw new AccesoNoPermitidoExcepcion(
                "El usuario no tiene una contraseña temporal pendiente de cambio.");

        try
        {
            await _proveedor.CambiarContrasenaAsync(
                comando.IdKeycloak, comando.Datos.NuevaContrasena, cancelacion);
        }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "Keycloak rechazó el cambio obligatorio. Usuario={NombreUsuario}, " +
                "Id={Id}, IdKeycloak={IdKeycloak}. Bandera se mantiene activa.",
                usuario.NombreUsuario.Valor, usuario.Id, comando.IdKeycloak);
            throw;
        }

        await _control.LimpiarDebeCambiarPorIdKeycloakAsync(comando.IdKeycloak, cancelacion);

        _registro.LogInformation(
            "Cambio obligatorio completado. Usuario={NombreUsuario}, Id={Id}, " +
            "IdKeycloak={IdKeycloak}.",
            usuario.NombreUsuario.Valor, usuario.Id, comando.IdKeycloak);

        return new CambiarContrasenaObligatoriaRespuestaDto
        {
            Mensaje = "Contraseña actualizada correctamente. Inicia sesión nuevamente.",
            RutaRedireccion = DtoMapeador.ResolverRutaPorRol(usuario.Rol)
        };
    }
}
