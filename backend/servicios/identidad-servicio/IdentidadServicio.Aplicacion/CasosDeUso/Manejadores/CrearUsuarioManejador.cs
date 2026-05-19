using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class CrearUsuarioManejador
    : IRequestHandler<CrearUsuarioComando, CrearUsuarioRespuestaDto>
{
    private readonly IRepositorioIdentidad _repositorio;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IProveedorFechaHora _reloj;
    private readonly FabricaEstrategiaCreacionUsuario _fabrica;
    private readonly IValidadorCrearUsuario _validador;
    private readonly ILogger<CrearUsuarioManejador> _registro;

    public CrearUsuarioManejador(
        IRepositorioIdentidad repositorio,
        IProveedorIdentidad proveedor,
        IProveedorFechaHora reloj,
        FabricaEstrategiaCreacionUsuario fabrica,
        IValidadorCrearUsuario validador,
        ILogger<CrearUsuarioManejador> registro)
    {
        _repositorio = repositorio;
        _proveedor = proveedor;
        _reloj = reloj;
        _fabrica = fabrica;
        _validador = validador;
        _registro = registro;
    }

    public async Task<CrearUsuarioRespuestaDto> Handle(
        CrearUsuarioComando comando, CancellationToken cancelacion)
    {
        var dto = comando.Datos;

        // 1) Validación reutilizable (campos + duplicados + reglas por rol).
        //    El validador normaliza el teléfono (sin espacios ni guiones) y, si
        //    hay errores, lanza ExcepcionValidacion → middleware → HTTP 400.
        await _validador.ValidarAsync(dto, cancelacion);

        // 2) Estrategia para el TipoUsuario (Administrador, Operador o Participante).
        var estrategia = _fabrica.Obtener(dto.TipoUsuario);

        // 3) Fecha vía IProveedorFechaHora.
        var fechaRegistro = _reloj.ObtenerFechaHoraUtc();

        // 4) Crear en Keycloak (username, correo, nombre y apellido separados;
        //    temporary = false).
        var datosIdentidad = new DatosCreacionUsuarioIdentidad(
            NombreUsuario: dto.NombreUsuario.Trim(),
            Correo: dto.Correo.Trim().ToLowerInvariant(),
            Contrasena: dto.Contrasena,
            Nombre: dto.Nombre.Trim(),
            Apellido: dto.Apellido.Trim());

        var idKeycloak = await _proveedor.CrearUsuarioAsync(datosIdentidad, cancelacion);

        try
        {
            await _proveedor.AsignarRolAsync(
                idKeycloak, estrategia.ObtenerRol().ToString(), cancelacion);

            var usuario = await estrategia.CrearUsuarioDominioAsync(dto, fechaRegistro, cancelacion);
            await estrategia.GuardarAsync(usuario, idKeycloak, _repositorio, cancelacion);

            var codigo = ObtenerCodigo(usuario);

            _registro.LogInformation(
                "Usuario {NombreUsuario} ({Correo}) creado con rol {Rol} y código {Codigo}.",
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
                    ? $"{usuario.Rol} registrado correctamente."
                    : $"{usuario.Rol} registrado correctamente. Código generado: {codigo}"
            };
        }
        catch
        {
            await CompensarKeycloakAsync(idKeycloak);
            throw;
        }
    }

    private static string? ObtenerCodigo(Dominio.Entidades.Usuario usuario) => usuario switch
    {
        Dominio.Entidades.Operador o => o.CodigoOperador,
        Dominio.Entidades.Administrador a => a.CodigoAdministrador,
        _ => null
    };

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
