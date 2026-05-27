using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

// HU13 — coordinador del caso de uso de eliminación permanente de un
// Operador por parte de un Administrador desde el panel web.
//
// Flujo (idéntico al de HU11 en su contrato con Keycloak + Postgres pero
// trabajando sobre el agregado Operador):
//  1. Buscar el Operador por id (devuelve null si no existe o si el id
//     pertenece a otro rol — Administrador o Participante).
//  2. Si no existe, error controlado (404 vía DatosUsuarioInvalidosExcepcion).
//  3. Preparar la eliminación en BD (sin SaveChanges).
//  4. Eliminar al usuario en Keycloak. 404 se trata como idempotente.
//  5. Si Keycloak responde OK, confirmar la transacción con la unidad
//     de trabajo. Si Keycloak falla, el scope EF se descarta y la base
//     queda intacta.
//
// La autorización por rol Administrador la garantiza la política de
// autorización del controlador. NO duplicamos la verificación de rol
// aquí porque el manejador no recibe el ClaimsPrincipal — confiamos en
// la política, igual que HU09 (modificar Operador).
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
        // 1) Cargar el Operador. ObtenerPorIdAsync devuelve null si el id
        //    no existe o si pertenece a otro rol (Administrador, Participante).
        //    Esto bloquea el intento de eliminar un Administrador o un
        //    Participante por este endpoint.
        var operador = await _repositorio.ObtenerPorIdAsync(comando.IdOperador, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un Operador con id {comando.IdOperador}.");

        // 2) Obtener IdKeycloak SIN tocar el tracker EF. Si por alguna razón
        //    el modelo no tuviera sub, abortamos antes de marcar el borrado:
        //    no podemos eliminar de Keycloak.
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

        // 3) Preparar eliminación en base de datos (sin confirmar).
        await _repositorio.EliminarAsync(operador, cancelacion);

        // 4) Eliminar en Keycloak. Si responde 404 la implementación lo
        //    trata como idempotente (la cuenta ya no existe allí) y
        //    devuelve sin error. Cualquier otra falla burbujea y el scope
        //    EF se descarta antes de confirmar la transacción.
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

        // 5) Confirmar la transacción en PostgreSQL.
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
