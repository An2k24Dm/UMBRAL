using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

// HU02 — registro administrativo de Operador o Administrador desde el panel.
//
// Tras el refactor de repositorios, el manejador depende SOLO de los puertos
// que necesita:
//  * IRepositorioUnicidadUsuario — duplicados antes de tocar Keycloak.
//  * IRepositorioOperadores / IRepositorioAdministradores — alta del agregado
//    concreto. La estrategia decide qué tipo de agregado crear; el manejador
//    decide en qué repositorio persistirlo por pattern matching.
//  * IUnidadTrabajoIdentidad — confirma el SaveChanges al cierre del flujo.
public sealed class CrearUsuarioManejador
    : IRequestHandler<CrearUsuarioComando, CrearUsuarioRespuestaDto>
{
    private readonly IRepositorioUnicidadUsuario _unicidad;
    private readonly IRepositorioOperadores _repositorioOperadores;
    private readonly IRepositorioAdministradores _repositorioAdministradores;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IProveedorFechaHora _reloj;
    private readonly FabricaEstrategiaCreacionUsuario _fabrica;
    private readonly IValidador<CrearUsuarioComando> _validador;
    private readonly ILogger<CrearUsuarioManejador> _registro;

    public CrearUsuarioManejador(
        IRepositorioUnicidadUsuario unicidad,
        IRepositorioOperadores repositorioOperadores,
        IRepositorioAdministradores repositorioAdministradores,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        IProveedorFechaHora reloj,
        FabricaEstrategiaCreacionUsuario fabrica,
        IValidador<CrearUsuarioComando> validador,
        ILogger<CrearUsuarioManejador> registro)
    {
        _unicidad = unicidad;
        _repositorioOperadores = repositorioOperadores;
        _repositorioAdministradores = repositorioAdministradores;
        _unidadTrabajo = unidadTrabajo;
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

        // 1) Validación de formato (sincrónica, sin acceso a base de datos).
        _validador.Validar(comando).LanzarSiHayErrores();

        // 2) Duplicados (asincrónicos contra IRepositorioUnicidadUsuario).
        await ValidarDuplicadosAsync(dto, cancelacion);

        // 3) Estrategia para el TipoUsuario.
        var estrategia = _fabrica.Obtener(dto.TipoUsuario);

        var fechaRegistro = _reloj.ObtenerFechaHoraUtc();

        // 4) Alta en Keycloak antes de tocar la base: si falla, compensamos.
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

            // 5) Mapeo a DatosCreacionUsuario. HU02 nunca lleva Alias: ese
            //    campo es exclusivo de HU03.
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

            // 6) Persistencia: el manejador escoge el repo concreto en función
            //    del tipo de agregado devuelto por la estrategia. Esto evita
            //    una fachada gigante o un Strategy de persistencia adicional.
            await PersistirAsync(usuario, idKeycloak, cancelacion);
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

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

    // HU02 — duplicados antes de tocar Keycloak. Se agregan todos los errores
    // detectados para que el frontend pueda resaltar los campos en conflicto
    // en una sola pasada.
    private async Task ValidarDuplicadosAsync(
        CrearUsuarioDto dto, CancellationToken cancelacion)
    {
        var resultado = ResultadoValidacion.Exitoso();

        if (await _unicidad.ExisteNombreUsuarioAsync(dto.NombreUsuario, cancelacion))
            resultado.Agregar(MensajesValidacionUsuario.CampoNombreUsuario,
                MensajesValidacionUsuario.NombreUsuarioDuplicado);

        if (await _unicidad.ExisteCorreoAsync(dto.Correo, cancelacion))
            resultado.Agregar(MensajesValidacionUsuario.CampoCorreo,
                MensajesValidacionUsuario.CorreoDuplicado);

        if (!string.IsNullOrWhiteSpace(dto.DatosContacto?.Telefono) &&
            await _unicidad.ExisteTelefonoAsync(dto.DatosContacto!.Telefono!, cancelacion))
            resultado.Agregar(MensajesValidacionUsuario.CampoTelefono,
                MensajesValidacionUsuario.TelefonoDuplicado);

        resultado.LanzarSiHayErrores();
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
