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

public sealed class ModificarOperadorManejador
    : IRequestHandler<ModificarOperadorComando, ModificarOperadorRespuestaDto>
{
    private readonly IRepositorioOperadores _repositorioOperadores;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IValidador<ModificarOperadorComando> _validador;
    private readonly IValidadorAsincrono<ModificarOperadorComando> _validadorUnicidad;
    private readonly AplicadorCambiosUsuario _aplicadorCambios;
    private readonly FabricaEstrategiaMapeoPerfilUsuario _fabricaMapeo;
    private readonly ILogger<ModificarOperadorManejador> _registro;

    public ModificarOperadorManejador(
        IRepositorioOperadores repositorioOperadores,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        IValidador<ModificarOperadorComando> validador,
        IValidadorAsincrono<ModificarOperadorComando> validadorUnicidad,
        AplicadorCambiosUsuario aplicadorCambios,
        FabricaEstrategiaMapeoPerfilUsuario fabricaMapeo,
        ILogger<ModificarOperadorManejador> registro)
    {
        _repositorioOperadores = repositorioOperadores;
        _unidadTrabajo = unidadTrabajo;
        _proveedor = proveedor;
        _validador = validador;
        _validadorUnicidad = validadorUnicidad;
        _aplicadorCambios = aplicadorCambios;
        _fabricaMapeo = fabricaMapeo;
        _registro = registro;
    }

    public async Task<ModificarOperadorRespuestaDto> Handle(
        ModificarOperadorComando comando, CancellationToken cancelacion)
    {
        // 1) Cargar Operador (404 si no existe o no es Operador).
        var operador = await _repositorioOperadores.ObtenerPorIdAsync(comando.IdOperador, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un Operador con id {comando.IdOperador}.");

        // 2) Validación de formato (sincrónica, sin I/O).
        _validador.Validar(comando).LanzarSiHayErrores();

        // 3) Validación de unicidad (asincrónica, excluye al propio Operador).
        (await _validadorUnicidad.ValidarAsync(comando, cancelacion)).LanzarSiHayErrores();

        // 4) Aplicar cambios al agregado mediante el servicio dedicado.
        var cambios = _aplicadorCambios.Aplicar(operador, comando.Datos);

        // 5) Si no hay cambios reales (ni de datos ni de contraseña), devolver
        //    respuesta indicándolo sin tocar repositorio ni Keycloak.
        if (!cambios.HuboCambiosDatosUsuario && !cambios.CambiaContrasena)
        {
            _registro.LogInformation(
                "Edición HU09 sin cambios para Operador {Id}.", operador.Id);
            return new ModificarOperadorRespuestaDto
            {
                HuboCambios = false,
                CamposActualizados = Array.Empty<string>(),
                Mensaje = "No había cambios para aplicar.",
                Operador = MapearPerfil(operador)
            };
        }

        // 6) Conseguir IdKeycloak. Si hay cambios de datos, ActualizarAsync
        //    deja los cambios listos en el contexto EF y devuelve el sub.
        //    Si solo cambia la contraseña, lo consultamos sin tocar el tracker.
        var idKeycloak = cambios.HuboCambiosDatosUsuario
            ? await _repositorioOperadores.ActualizarAsync(operador, cancelacion)
            : await _repositorioOperadores.ObtenerIdKeycloakAsync(operador.Id, cancelacion);

        if (cambios.RequiereSincronizarKeycloak && string.IsNullOrWhiteSpace(idKeycloak))
        {
            _registro.LogError(
                "Operador {Id} no tiene IdKeycloak asociado: imposible sincronizar con Keycloak.",
                operador.Id);
            throw new InvalidOperationException(
                $"El Operador {operador.Id} no tiene IdKeycloak asociado. " +
                "No se puede sincronizar la actualización con Keycloak.");
        }

        // 7) Contraseña primero — si falla, no quedan ni datos en Keycloak
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
                    "Keycloak rechazó el cambio de contraseña del Operador {Id} (KC={Kc}). " +
                    "Los cambios NO se persisten en base de datos.",
                    operador.Id, idKeycloak);
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
                    "Keycloak rechazó la actualización del Operador {Id} (KC={Kc}). " +
                    "Los cambios NO se persisten en base de datos.",
                    operador.Id, idKeycloak);
                throw;
            }
        }

        // 9) Confirmar PostgreSQL solo si hubo cambios de datos. Si solo
        //    cambió la contraseña, no hay nada que guardar en BD.
        if (cambios.RequiereGuardarBaseDatos)
        {
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        }

        // 10) Armar la lista informativa de campos cambiados. "contrasena" se
        //     incluye SIN valor; jamás se devuelve la contraseña en claro.
        var camposRespuesta = new List<string>(cambios.CamposActualizados);
        if (cambios.CambiaContrasena) camposRespuesta.Add("contrasena");

        _registro.LogInformation(
            "Operador {Id} actualizado. Campos: {Campos}.",
            operador.Id, string.Join(",", camposRespuesta));

        return new ModificarOperadorRespuestaDto
        {
            HuboCambios = true,
            CamposActualizados = camposRespuesta,
            Mensaje = "Operador actualizado correctamente.",
            Operador = MapearPerfil(operador)
        };
    }

    private PerfilOperadorDto MapearPerfil(Operador operador)
        => (PerfilOperadorDto)_fabricaMapeo.Mapear(operador);
}
