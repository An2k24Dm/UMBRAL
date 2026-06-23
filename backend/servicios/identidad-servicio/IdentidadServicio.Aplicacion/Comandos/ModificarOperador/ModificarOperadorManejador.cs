using IdentidadServicio.Aplicacion.Fabricas;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Aplicacion.Validaciones;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.Comandos.ModificarOperador;

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
        var operador = await _repositorioOperadores.ObtenerPorIdAsync(comando.IdOperador, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un Operador con id {comando.IdOperador}.");

        _validador.Validar(comando).LanzarSiHayErrores();

        (await _validadorUnicidad.ValidarAsync(comando, cancelacion)).LanzarSiHayErrores();

        var cambios = _aplicadorCambios.Aplicar(operador, comando.Datos);

        if (!cambios.HuboCambiosDatosUsuario)
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

        var idKeycloak = await _repositorioOperadores.ActualizarAsync(operador, cancelacion);

        if (cambios.RequiereSincronizarKeycloak && string.IsNullOrWhiteSpace(idKeycloak))
        {
            _registro.LogError(
                "Operador {Id} no tiene IdKeycloak asociado: imposible sincronizar con Keycloak.",
                operador.Id);
            throw new InvalidOperationException(
                $"El Operador {operador.Id} no tiene IdKeycloak asociado. " +
                "No se puede sincronizar la actualización con Keycloak.");
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
                    "Keycloak rechazó la actualización del Operador {Id} (KC={Kc}). " +
                    "Los cambios NO se persisten en base de datos.",
                    operador.Id, idKeycloak);
                throw;
            }
        }

        if (cambios.RequiereGuardarBaseDatos)
        {
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        }

        var camposRespuesta = new List<string>(cambios.CamposActualizados);

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
