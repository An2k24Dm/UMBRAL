using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.Comandos.EliminarCuentaParticipante;

// HU11 — coordinador del caso de uso de eliminación permanente de la
// cuenta del Participante autenticado desde la app móvil.
//
// Reglas:
//  * El Participante es identificado por el sub del token (IdKeycloak), no
//    por un id que envíe el cliente.
//  * Solo se elimina si Estado == Activo (regla de dominio expresada en
//    Participante.PuedeEliminarCuenta).
//  * Primero se prepara la eliminación en la base de datos (sin
//    SaveChanges), después se llama a Keycloak. Si Keycloak responde OK
//    (incluso 404, que se trata como idempotente) recién entonces se
//    confirma con la unidad de trabajo. Si Keycloak falla, el scope EF se
//    descarta y la base de datos queda intacta.
public sealed class EliminarCuentaParticipanteManejador
    : IRequestHandler<EliminarCuentaParticipanteComando, EliminarCuentaParticipanteRespuestaDto>
{
    private readonly IRepositorioParticipantes _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IProveedorIdentidad _proveedor;
    private readonly ILogger<EliminarCuentaParticipanteManejador> _registro;

    public EliminarCuentaParticipanteManejador(
        IRepositorioParticipantes repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        ILogger<EliminarCuentaParticipanteManejador> registro)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _proveedor = proveedor;
        _registro = registro;
    }

    public async Task<EliminarCuentaParticipanteRespuestaDto> Handle(
        EliminarCuentaParticipanteComando comando, CancellationToken cancelacion)
    {
        // 1) Identidad: el sub del token debe estar presente. Si no, el
        //    middleware ya debería haber respondido 401, pero defendemos
        //    contra escenarios donde el endpoint se llama desde otro cliente.
        if (string.IsNullOrWhiteSpace(comando.IdKeycloak))
            throw new DatosUsuarioInvalidosExcepcion(
                "No se pudo determinar la identidad del Participante autenticado.");

        // 2) Buscar el Participante asociado al sub del token. Si no existe,
        //    error controlado (404 vía DatosUsuarioInvalidosExcepcion). Esto
        //    también cubre el caso "el rol del token es Participante pero el
        //    sub no corresponde a ningún Participante real en la base":
        //    Administradores y Operadores tienen otros roles y no son
        //    devueltos por este repositorio.
        var participante = await _repositorio.ObtenerPorIdKeycloakAsync(
            comando.IdKeycloak, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                "No existe un Participante asociado al usuario autenticado.");

        // 3) Regla de dominio: solo un Participante Activo puede eliminarse.
        if (!participante.PuedeEliminarCuenta())
        {
            _registro.LogWarning(
                "Participante {Id} intentó eliminar su cuenta en estado {Estado}.",
                participante.Id, participante.Estado);
            throw new CuentaDesactivadaExcepcion();
        }

        // 4) Preparar eliminación en base de datos (sin confirmar todavía).
        await _repositorio.EliminarAsync(participante, cancelacion);

        // 5) Eliminar al usuario en Keycloak. Si responde 404 la
        //    implementación lo trata como idempotente (la cuenta ya no
        //    existe en Keycloak) y devuelve sin error. Cualquier otra
        //    falla burbujea como excepción y la transacción EF NO se
        //    confirma — el scope se descarta al salir del manejador.
        try
        {
            await _proveedor.EliminarUsuarioAsync(comando.IdKeycloak, cancelacion);
        }
        catch (Exception ex)
        {
            _registro.LogError(ex,
                "Keycloak rechazó la eliminación del Participante {Id}. " +
                "La base de datos NO queda eliminada.",
                participante.Id);
            throw;
        }

        // 6) Confirmar la transacción de PostgreSQL.
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "Participante {Id} eliminado permanentemente (Keycloak + PostgreSQL).",
            participante.Id);

        return new EliminarCuentaParticipanteRespuestaDto
        {
            Eliminada = true,
            Mensaje = "Tu cuenta fue eliminada permanentemente."
        };
    }
}
