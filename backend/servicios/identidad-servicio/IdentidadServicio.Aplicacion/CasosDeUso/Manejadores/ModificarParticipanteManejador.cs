using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

// HU10 — coordinador del caso de uso de edición del propio perfil del
// Participante desde la app móvil.
//
// Reutiliza las piezas comunes con HU09:
//  * AplicadorCambiosUsuario      — detección y aplicación de cambios.
//  * ResultadoCambiosUsuario      — diff resultante.
//  * IValidador<TComando>         — validación de formato (HU10-específico).
//  * IValidadorAsincrono<TComando>— unicidad (excluye al propio Participante).
//  * IUnidadTrabajoIdentidad      — SaveChanges atómico.
//  * IProveedorIdentidad          — Keycloak (datos + contraseña).
//
// Difiere de HU09 en:
//  * El Participante es identificado por sub/IdKeycloak del token, no por id.
//  * Se usa IRepositorioParticipantes en lugar de IRepositorioOperadores.
//  * La política de autorización del controlador exige rol Participante.
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
        // 1) Identificar al Participante autenticado por el sub del token.
        //    Si no existe Participante asociado a ese sub, devolvemos error
        //    controlado (404 vía DatosUsuarioInvalidosExcepcion).
        if (string.IsNullOrWhiteSpace(comando.IdKeycloak))
            throw new DatosUsuarioInvalidosExcepcion(
                "No se pudo determinar la identidad del Participante autenticado.");

        var participante = await _repositorio.ObtenerPorIdKeycloakAsync(
            comando.IdKeycloak, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                "No existe un Participante asociado al usuario autenticado.");

        // 2) Validación de formato (sincrónica, sin I/O).
        _validador.Validar(comando).LanzarSiHayErrores();

        // 3) Validación de unicidad. Para que excluya al propio Participante
        //    le pasamos el id resuelto vía el comando.
        comando.IdParticipanteActual = participante.Id;
        (await _validadorUnicidad.ValidarAsync(comando, cancelacion)).LanzarSiHayErrores();

        // 4) Aplicar cambios al agregado mediante el servicio común.
        var cambios = _aplicadorCambios.Aplicar(participante, comando.Datos);

        // 5) Sin cambios → respuesta sin tocar repositorio ni Keycloak.
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

        // 6) Conseguir IdKeycloak. Si hay cambios de datos ActualizarAsync
        //    prepara el tracker EF y devuelve el sub. Si solo es contraseña,
        //    usamos el sub del comando (que ya identificó al usuario).
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

        // 7) Contraseña primero — si falla, no quedan datos ni en Keycloak
        //    ni cambios persistidos en BD.
        if (cambios.CambiaContrasena)
        {
            try
            {
                await _proveedor.CambiarContrasenaAsync(
                    idKeycloak!, cambios.NuevaContrasena!, temporal: false, cancelacion);
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

        // 8) Datos en Keycloak (username / email / firstName / lastName).
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

        // 9) Confirmar PostgreSQL solo si hubo cambios de datos.
        if (cambios.RequiereGuardarBaseDatos)
        {
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        }

        // 10) Armar lista informativa. "contrasena" sin valor si aplica.
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
