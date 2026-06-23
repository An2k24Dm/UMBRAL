using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Generadores;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.Comandos.CrearUsuario;

public sealed class CrearUsuarioManejador
    : IRequestHandler<CrearUsuarioComando, CrearUsuarioRespuestaDto>
{
    private readonly ValidadorUnicidadUsuario _validadorUnicidad;
    private readonly IRepositorioOperadores _repositorioOperadores;
    private readonly IRepositorioAdministradores _repositorioAdministradores;
    private readonly IRepositorioControlContrasenaTemporal _controlContrasena;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IProveedorFechaHora _reloj;
    private readonly FabricaEstrategiaCreacionUsuario _fabrica;
    private readonly IValidador<CrearUsuarioComando> _validador;
    private readonly IGeneradorContrasenaTemporal _generadorContrasena;
    private readonly IServicioCorreo _correo;
    private readonly ILogger<CrearUsuarioManejador> _registro;

    public CrearUsuarioManejador(
        ValidadorUnicidadUsuario validadorUnicidad,
        IRepositorioOperadores repositorioOperadores,
        IRepositorioAdministradores repositorioAdministradores,
        IRepositorioControlContrasenaTemporal controlContrasena,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        IProveedorFechaHora reloj,
        FabricaEstrategiaCreacionUsuario fabrica,
        IValidador<CrearUsuarioComando> validador,
        IGeneradorContrasenaTemporal generadorContrasena,
        IServicioCorreo correo,
        ILogger<CrearUsuarioManejador> registro)
    {
        _validadorUnicidad = validadorUnicidad;
        _repositorioOperadores = repositorioOperadores;
        _repositorioAdministradores = repositorioAdministradores;
        _controlContrasena = controlContrasena;
        _unidadTrabajo = unidadTrabajo;
        _proveedor = proveedor;
        _reloj = reloj;
        _fabrica = fabrica;
        _validador = validador;
        _generadorContrasena = generadorContrasena;
        _correo = correo;
        _registro = registro;
    }

    public async Task<CrearUsuarioRespuestaDto> Handle(
        CrearUsuarioComando comando, CancellationToken cancelacion)
    {
        var dto = comando.Datos;

        _validador.Validar(comando).LanzarSiHayErrores();

        await _validadorUnicidad.ValidarCreacionUsuarioAsync(dto, cancelacion);

        var estrategia = _fabrica.Obtener(dto.TipoUsuario);

        var fechaRegistro = _reloj.ObtenerFechaHoraUtc();

        var contrasenaTemporal = _generadorContrasena.Generar();

        var datosIdentidad = new DatosCreacionUsuarioIdentidad(
            NombreUsuario: dto.NombreUsuario.Trim(),
            Correo: dto.Correo.Trim().ToLowerInvariant(),
            Contrasena: contrasenaTemporal,
            Nombre: dto.Nombre.Trim(),
            Apellido: dto.Apellido.Trim());

        var idKeycloak = await _proveedor.CrearUsuarioAsync(datosIdentidad, cancelacion);

        try
        {
            await _proveedor.AsignarRolAsync(
                idKeycloak, estrategia.ObtenerRol().ToString(), cancelacion);

            var datosCreacion = new DatosCreacionUsuario
            {
                TipoUsuario = dto.TipoUsuario,
                NombreUsuario = dto.NombreUsuario,
                Correo = dto.Correo,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Sexo = dto.Sexo,
                FechaNacimiento = DatosCreacionUsuario.NormalizarFechaNacimiento(dto.FechaNacimiento),
                DatosContacto = dto.DatosContacto,
                Alias = null
            };

            var usuario = await estrategia.CrearUsuarioDominioAsync(
                datosCreacion, fechaRegistro, cancelacion);

            await PersistirAsync(usuario, idKeycloak, cancelacion);
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

            await _controlContrasena.MarcarDebeCambiarPorIdAsync(usuario.Id, cancelacion);

            var codigo = ObtenerCodigo(usuario);

            await EnviarCorreoCreacionAsync(usuario, contrasenaTemporal, cancelacion);

            _registro.LogInformation(
                "Usuario {NombreUsuario} ({Correo}) creado con rol {Rol} y código {Codigo}. " +
                "Se envió contraseña temporal por correo.",
                usuario.NombreUsuario.Valor, usuario.Correo.Valor, usuario.Rol, codigo);

            return new CrearUsuarioRespuestaDto
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario.Valor,
                Correo = usuario.Correo.Valor,
                Rol = usuario.Rol.ToString(),
                Estado = usuario.Estado.ToString(),
                Codigo = codigo,
                Mensaje = codigo is null
                    ? $"{usuario.Rol} creado correctamente. Se envió una contraseña temporal a su correo electrónico."
                    : $"{usuario.Rol} creado correctamente. Código generado: {codigo}. Se envió una contraseña temporal a su correo electrónico."
            };
        }
        catch
        {
            await CompensarKeycloakAsync(idKeycloak);
            throw;
        }
    }

    private Task PersistirAsync(Usuario usuario, string idKeycloak, CancellationToken c) =>
        usuario switch
        {
            Operador op => _repositorioOperadores.AgregarAsync(op, idKeycloak, c),
            Administrador ad => _repositorioAdministradores.AgregarAsync(ad, idKeycloak, c),
            _ => throw new InvalidOperationException(
                $"HU02 no soporta persistencia para el rol {usuario.Rol}.")
        };

    private static string? ObtenerCodigo(Usuario usuario) => usuario switch
    {
        Operador o => o.CodigoOperador,
        Administrador a => a.CodigoAdministrador,
        _ => null
    };

    private async Task EnviarCorreoCreacionAsync(
        Usuario usuario, string contrasenaTemporal, CancellationToken cancelacion)
    {
        try
        {
            var cuerpo = MensajesContrasenaTemporal.CuerpoCreacion(
                nombreCompleto: $"{usuario.NombrePersona.Nombre} {usuario.NombrePersona.Apellido}".Trim(),
                nombreUsuario: usuario.NombreUsuario.Valor,
                correoAcceso: usuario.Correo.Valor,
                contrasenaTemporal: contrasenaTemporal,
                rol: usuario.Rol.ToString());

            await _correo.EnviarAsync(
                usuario.Correo.Valor,
                MensajesContrasenaTemporal.AsuntoCreacion,
                cuerpo,
                cancelacion);
        }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "No fue posible enviar el correo de creación al usuario {Id}. " +
                "Use el endpoint de reseteo para reintentar.",
                usuario.Id);
        }
    }

    private async Task CompensarKeycloakAsync(string idKeycloak)
    {
        try { await _proveedor.EliminarUsuarioAsync(idKeycloak, CancellationToken.None); }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "Compensación fallida: requiere limpieza manual de {Id} en Keycloak.", idKeycloak);
        }
    }
}
