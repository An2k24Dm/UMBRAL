using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.EliminarCuentaParticipante;

public sealed class EliminarCuentaParticipanteManejador
    : IRequestHandler<EliminarCuentaParticipanteComando, EliminarCuentaParticipanteRespuestaDto>
{
    private readonly IRepositorioParticipantes _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IProveedorIdentidad _proveedor;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public EliminarCuentaParticipanteManejador(
        IRepositorioParticipantes repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IProveedorIdentidad proveedor,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _proveedor = proveedor;
        _registroLogs = registroLogs;
    }

    public async Task<EliminarCuentaParticipanteRespuestaDto> Handle(
        EliminarCuentaParticipanteComando comando, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(comando.IdKeycloak))
            throw new DatosUsuarioInvalidosExcepcion(
                "No se pudo determinar la identidad del Participante autenticado.");

        var participante = await _repositorio.ObtenerPorIdKeycloakAsync(
            comando.IdKeycloak, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                "No existe un Participante asociado al usuario autenticado.");

        if (!participante.PuedeEliminarCuenta())
        {
            _registroLogs.Advertencia(
                evento: "EliminacionCuentaEstadoInvalido",
                descripcion: "El Participante intentó eliminar su cuenta desde un estado no permitido.",
                propiedades: new Dictionary<string, object?>
                {
                    ["ParticipanteId"] = participante.Id,
                    ["Estado"] = participante.Estado.ToString()
                });
            throw new CuentaDesactivadaExcepcion();
        }

        await _repositorio.EliminarAsync(participante, cancelacion);

        try
        {
            await _proveedor.EliminarUsuarioAsync(comando.IdKeycloak, cancelacion);
        }
        catch (Exception ex)
        {
            _registroLogs.Error(
                excepcion: ex,
                evento: "EliminacionCuentaKeycloakFallida",
                descripcion: "Keycloak rechazó la eliminación del Participante. La base de datos NO queda eliminada.",
                propiedades: new Dictionary<string, object?>
                {
                    ["ParticipanteId"] = participante.Id
                });
            throw;
        }

        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registroLogs.Informacion(
            evento: "CuentaParticipanteEliminada",
            descripcion: "Participante eliminó su cuenta correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["ParticipanteId"] = participante.Id
            });

        return new EliminarCuentaParticipanteRespuestaDto
        {
            Eliminada = true,
            Mensaje = "Tu cuenta fue eliminada permanentemente."
        };
    }
}
