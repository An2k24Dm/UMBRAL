using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

// HU03 — registro público de Participante desde la app móvil. Reutiliza el
// Strategy/Factory de creación, pero solo necesita el repositorio de
// Participantes y la unidad de trabajo: ya no depende de una fachada gigante.
public sealed class RegistrarParticipanteManejador
    : IRequestHandler<RegistrarParticipanteComando, CrearUsuarioRespuestaDto>
{
    private readonly IRepositorioUnicidadUsuario _unicidad;
    private readonly IRepositorioParticipantes _repositorioParticipantes;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IProveedorFechaHora _reloj;
    private readonly FabricaEstrategiaCreacionUsuario _fabrica;
    private readonly IValidador<RegistrarParticipanteComando> _validador;
    private readonly ILogger<RegistrarParticipanteManejador> _registro;

    public RegistrarParticipanteManejador(
        IRepositorioUnicidadUsuario unicidad,
        IRepositorioParticipantes repositorioParticipantes,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        IProveedorFechaHora reloj,
        FabricaEstrategiaCreacionUsuario fabrica,
        IValidador<RegistrarParticipanteComando> validador,
        ILogger<RegistrarParticipanteManejador> registro)
    {
        _unicidad = unicidad;
        _repositorioParticipantes = repositorioParticipantes;
        _unidadTrabajo = unidadTrabajo;
        _proveedor = proveedor;
        _reloj = reloj;
        _fabrica = fabrica;
        _validador = validador;
        _registro = registro;
    }

    public async Task<CrearUsuarioRespuestaDto> Handle(
        RegistrarParticipanteComando comando, CancellationToken cancelacion)
    {
        var dto = comando.Datos;

        _validador.Validar(comando).LanzarSiHayErrores();
        await ValidarDuplicadosAsync(dto, cancelacion);

        var estrategia = _fabrica.Obtener(RolUsuario.Participante);
        var fechaRegistro = _reloj.ObtenerFechaHoraUtc();

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

            var datosCreacion = new DatosCreacionUsuario
            {
                TipoUsuario = RolUsuario.Participante,
                Alias = dto.Alias.Trim(),
                NombreUsuario = dto.NombreUsuario,
                Correo = dto.Correo,
                Nombre = dto.Nombre,
                Apellido = dto.Apellido,
                Sexo = dto.Sexo,
                FechaNacimiento = DatosCreacionUsuario.NormalizarFechaNacimiento(dto.FechaNacimiento),
                DatosContacto = dto.DatosContacto
            };

            var usuario = await estrategia.CrearUsuarioDominioAsync(
                datosCreacion, fechaRegistro, cancelacion);

            // La estrategia siempre devuelve Participante en HU03; aseguramos
            // explícitamente el tipo antes de delegar al repositorio.
            var participante = (Participante)usuario;
            await _repositorioParticipantes.AgregarAsync(participante, idKeycloak, cancelacion);
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

            _registro.LogInformation(
                "Participante {NombreUsuario} ({Correo}) registrado desde la app móvil.",
                usuario.NombreUsuario.Valor, usuario.Correo.Valor);

            return new CrearUsuarioRespuestaDto
            {
                Id = usuario.Id,
                NombreUsuario = usuario.NombreUsuario.Valor,
                Correo = usuario.Correo.Valor,
                Rol = usuario.Rol.ToString(),
                Estado = usuario.Estado.ToString(),
                Codigo = null,
                Mensaje = "Participante registrado correctamente."
            };
        }
        catch
        {
            await CompensarKeycloakAsync(idKeycloak);
            throw;
        }
    }

    // HU03 — duplicados específicos del registro público (incluye alias).
    private async Task ValidarDuplicadosAsync(
        RegistrarParticipanteDto dto, CancellationToken cancelacion)
    {
        var resultado = ResultadoValidacion.Exitoso();

        if (await _unicidad.ExisteAliasAsync(dto.Alias, cancelacion))
            resultado.Agregar(MensajesValidacionUsuario.CampoAlias,
                MensajesValidacionUsuario.AliasDuplicado);

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
