using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.EliminarOperador;

public sealed class EliminarOperadorManejador
    : IRequestHandler<EliminarOperadorComando, EliminarOperadorRespuestaDto>
{
    private readonly IRepositorioOperadores _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public EliminarOperadorManejador(
        IRepositorioOperadores repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _proveedor = proveedor;
        _registroLogs = registroLogs;
    }

    public async Task<EliminarOperadorRespuestaDto> Handle(
        EliminarOperadorComando comando, CancellationToken cancelacion)
    {
        var operador = await _repositorio.ObtenerPorIdAsync(comando.IdOperador, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un Operador con id {comando.IdOperador}.");

        var idKeycloak = await _repositorio.ObtenerIdKeycloakAsync(operador.Id, cancelacion);
        if (string.IsNullOrWhiteSpace(idKeycloak))
        {
            _registroLogs.Advertencia(
                evento: "OperadorSinIdKeycloak",
                descripcion: "El Operador no tiene IdKeycloak asociado: imposible sincronizar la eliminación con Keycloak.",
                propiedades: new Dictionary<string, object?>
                {
                    ["OperadorId"] = operador.Id
                });
            throw new InvalidOperationException(
                $"El Operador {operador.Id} no tiene IdKeycloak asociado. " +
                "No se puede sincronizar la eliminación con Keycloak.");
        }

        await _repositorio.EliminarAsync(operador, cancelacion);

        try
        {
            await _proveedor.EliminarUsuarioAsync(idKeycloak, cancelacion);
        }
        catch (Exception ex)
        {
            _registroLogs.Error(
                excepcion: ex,
                evento: "EliminacionOperadorKeycloakFallida",
                descripcion: "Keycloak rechazó la eliminación del Operador. La base de datos NO queda eliminada.",
                propiedades: new Dictionary<string, object?>
                {
                    ["OperadorId"] = operador.Id,
                    ["IdKeycloak"] = idKeycloak
                });
            throw;
        }

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registroLogs.Informacion(
            evento: "OperadorEliminado",
            descripcion: "Administrador eliminó un operador correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["OperadorId"] = operador.Id
            });

        return new EliminarOperadorRespuestaDto
        {
            IdOperador = operador.Id,
            Eliminado = true,
            Mensaje = "Operador eliminado permanentemente."
        };
    }
}
