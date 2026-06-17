using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class EliminarOperadorManejador
    : IRequestHandler<EliminarOperadorComando, EliminarOperadorRespuestaDto>
{
    private readonly IRepositorioOperadores _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IProveedorIdentidad _proveedor;
    private readonly ILogger<EliminarOperadorManejador> _registro;

    public EliminarOperadorManejador(
        IRepositorioOperadores repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        ILogger<EliminarOperadorManejador> registro)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _proveedor = proveedor;
        _registro = registro;
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
            _registro.LogError(
                "Operador {Id} no tiene IdKeycloak asociado: imposible sincronizar la eliminación con Keycloak.",
                operador.Id);
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
            _registro.LogError(ex,
                "Keycloak rechazó la eliminación del Operador {Id} (KC={Kc}). " +
                "La base de datos NO queda eliminada.",
                operador.Id, idKeycloak);
            throw;
        }

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "Operador {Id} eliminado permanentemente (Keycloak + PostgreSQL) por Administrador.",
            operador.Id);

        return new EliminarOperadorRespuestaDto
        {
            IdOperador = operador.Id,
            Eliminado = true,
            Mensaje = "Operador eliminado permanentemente."
        };
    }
}
