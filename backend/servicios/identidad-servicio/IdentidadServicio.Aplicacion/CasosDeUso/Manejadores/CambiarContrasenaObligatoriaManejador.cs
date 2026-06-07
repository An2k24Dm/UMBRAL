using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Mapeadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

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

        // 1) Validar nueva contraseña (formato + coincidencia).
        _validador.Validar(comando).LanzarSiHayErrores();

        // 2) Cargar usuario por IdKeycloak. Si no existe en UMBRAL,
        //    error. Si es Participante, este endpoint no aplica.
        var usuario = await _lectura.ObtenerPorIdKeycloakAsync(comando.IdKeycloak, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                "El usuario no está registrado en UMBRAL.");

        if (usuario.Rol != RolUsuario.Operador && usuario.Rol != RolUsuario.Administrador)
            throw new AccesoNoPermitidoExcepcion(
                "El cambio obligatorio de contraseña solo aplica a Operador o Administrador.");

        // 3) Verificar que efectivamente proviene del flujo temporal. Si la
        //    bandera está apagada, no hay nada que cambiar por esta vía.
        var debeCambiar = await _control.ObtenerDebeCambiarPorIdKeycloakAsync(
            comando.IdKeycloak, cancelacion);
        if (!debeCambiar)
            throw new AccesoNoPermitidoExcepcion(
                "El usuario no tiene una contraseña temporal pendiente de cambio.");

        // 4) Keycloak primero. CambiarContrasenaAsync ya hace
        //    EnsureSuccessStatusCode internamente y no marca la credencial
        //    como temporal (UMBRAL nunca usa temporary:true).
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

        // 5) Limpiar bandera. Si esto falla la próxima sesión seguirá
        //    pidiendo cambio: el usuario podrá completar el flujo otra
        //    vez con la nueva contraseña.
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
