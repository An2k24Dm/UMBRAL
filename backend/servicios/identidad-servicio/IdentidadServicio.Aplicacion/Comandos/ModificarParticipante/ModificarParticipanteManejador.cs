using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<ModificarParticipanteManejador> _registro;

    public ModificarParticipanteManejador(
        IRepositorioParticipantes repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        IValidador<ModificarParticipanteComando> validador,
        IValidadorAsincrono<ModificarParticipanteComando> validadorUnicidad,
        AplicadorCambiosUsuario aplicadorCambios,
        FabricaEstrategiaMapeoPerfilUsuario fabricaMapeo,
        ILogger<ModificarParticipanteManejador> registro)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _proveedor = proveedor;
        _validador = validador;
        _validadorUnicidad = validadorUnicidad;
        _aplicadorCambios = aplicadorCambios;
        _fabricaMapeo = fabricaMapeo;
        _registro = registro;
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
            _registro.LogInformation(
                "Edición HU10 sin cambios para Participante {Id}.", participante.Id);
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
            _registro.LogError(
                "Participante {Id} no tiene IdKeycloak asociado: imposible sincronizar con Keycloak.",
                participante.Id);
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
                _registro.LogError(ex,
                    "Keycloak rechazó el cambio de contraseña del Participante {Id}. " +
                    "Los cambios NO se persisten en base de datos.",
                    participante.Id);
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
                _registro.LogError(ex,
                    "Keycloak rechazó la actualización del Participante {Id}. " +
                    "Los cambios NO se persisten en base de datos.",
                    participante.Id);
                throw;
            }
        }

        if (cambios.RequiereGuardarBaseDatos)
        {
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        }

        var camposRespuesta = new List<string>(cambios.CamposActualizados);
        if (cambios.CambiaContrasena) camposRespuesta.Add("contrasena");

        _registro.LogInformation(
            "Participante {Id} actualizado. Campos: {Campos}.",
            participante.Id, string.Join(",", camposRespuesta));

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
