using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.ModificarParticipante;

public sealed class ModificarParticipanteManejador
    : IRequestHandler<ModificarParticipanteComando, ModificarParticipanteRespuestaDto>
{
    private readonly IRepositorioParticipantes _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IValidador<ModificarParticipanteComando> _validador;
    private readonly IValidadorAsincrono<ModificarParticipanteComando> _validadorUnicidad;
    private readonly AplicadorCambiosUsuario _aplicadorCambios;
    private readonly FabricaEstrategiaMapeoPerfilUsuario _fabricaMapeo;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ModificarParticipanteManejador(
        IRepositorioParticipantes repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        IValidador<ModificarParticipanteComando> validador,
        IValidadorAsincrono<ModificarParticipanteComando> validadorUnicidad,
        AplicadorCambiosUsuario aplicadorCambios,
        FabricaEstrategiaMapeoPerfilUsuario fabricaMapeo,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _proveedor = proveedor;
        _validador = validador;
        _validadorUnicidad = validadorUnicidad;
        _aplicadorCambios = aplicadorCambios;
        _fabricaMapeo = fabricaMapeo;
        _registroLogs = registroLogs;
    }

    public async Task<ModificarParticipanteRespuestaDto> Handle(
        ModificarParticipanteComando comando, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(comando.IdKeycloak))
            throw new DatosUsuarioInvalidosExcepcion(
                "No se pudo determinar la identidad del Participante autenticado.");

        var participante = await _repositorio.ObtenerPorIdKeycloakAsync(
            comando.IdKeycloak, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                "No existe un Participante asociado al usuario autenticado.");

        _validador.Validar(comando).LanzarSiHayErrores();

        comando.IdParticipanteActual = participante.Id;
        (await _validadorUnicidad.ValidarAsync(comando, cancelacion)).LanzarSiHayErrores();

        var cambios = _aplicadorCambios.Aplicar(participante, comando.Datos);

        if (!cambios.HuboCambiosDatosUsuario && !cambios.CambiaContrasena)
        {
            _registroLogs.Informacion(
                evento: "ParticipanteEdicionSinCambios",
                descripcion: "Edición de Participante sin cambios para aplicar.",
                propiedades: new Dictionary<string, object?>
                {
                    ["ParticipanteId"] = participante.Id
                });
            return new ModificarParticipanteRespuestaDto
            {
                HuboCambios = false,
                CamposActualizados = Array.Empty<string>(),
                Mensaje = "No había cambios para aplicar.",
                Participante = MapearPerfil(participante)
            };
        }

        string? idKeycloak = cambios.HuboCambiosDatosUsuario
            ? await _repositorio.ActualizarAsync(participante, cancelacion)
            : comando.IdKeycloak;

        if (cambios.RequiereSincronizarKeycloak && string.IsNullOrWhiteSpace(idKeycloak))
        {
            _registroLogs.Advertencia(
                evento: "ParticipanteSinIdKeycloak",
                descripcion: "El Participante no tiene IdKeycloak asociado: imposible sincronizar con Keycloak.",
                propiedades: new Dictionary<string, object?>
                {
                    ["ParticipanteId"] = participante.Id
                });
            throw new InvalidOperationException(
                $"El Participante {participante.Id} no tiene IdKeycloak asociado. " +
                "No se puede sincronizar la actualización con Keycloak.");
        }

        if (cambios.CambiaContrasena)
        {
            try
            {
                await _proveedor.CambiarContrasenaAsync(
                    idKeycloak!, cambios.NuevaContrasena!, cancelacion);
            }
            catch (Exception ex)
            {
                _registroLogs.Error(
                    excepcion: ex,
                    evento: "CambioContrasenaParticipanteKeycloakFallido",
                    descripcion: "Keycloak rechazó el cambio de contraseña del Participante. Los cambios NO se persisten en base de datos.",
                    propiedades: new Dictionary<string, object?>
                    {
                        ["ParticipanteId"] = participante.Id
                    });
                throw;
            }
        }

        if (cambios.DatosKeycloak.TieneCambios)
        {
            try
            {
                await _proveedor.ActualizarUsuarioAsync(
                    idKeycloak!, cambios.DatosKeycloak, cancelacion);
            }
            catch (Exception ex)
            {
                _registroLogs.Error(
                    excepcion: ex,
                    evento: "ModificacionParticipanteKeycloakFallida",
                    descripcion: "Keycloak rechazó la actualización del Participante. Los cambios NO se persisten en base de datos.",
                    propiedades: new Dictionary<string, object?>
                    {
                        ["ParticipanteId"] = participante.Id
                    });
                throw;
            }
        }

        if (cambios.RequiereGuardarBaseDatos)
        {
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        }

        var camposRespuesta = new List<string>(cambios.CamposActualizados);
        if (cambios.CambiaContrasena) camposRespuesta.Add("contrasena");

        _registroLogs.Informacion(
            evento: "ParticipanteModificado",
            descripcion: "Participante modificó su perfil correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["ParticipanteId"] = participante.Id,
                ["Campos"] = string.Join(",", camposRespuesta)
            });

        return new ModificarParticipanteRespuestaDto
        {
            HuboCambios = true,
            CamposActualizados = camposRespuesta,
            Mensaje = "Perfil actualizado correctamente.",
            Participante = MapearPerfil(participante)
        };
    }

    private PerfilParticipanteDto MapearPerfil(Participante participante)
        => (PerfilParticipanteDto)_fabricaMapeo.Mapear(participante);
}
