using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.DesactivarParticipante;

public sealed class DesactivarParticipanteManejador
    : IRequestHandler<DesactivarParticipanteComando, CambiarEstadoUsuarioRespuestaDto>
{
    private readonly IAutorizadorUsuarioActivo _autorizador;
    private readonly IRepositorioParticipantes _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public DesactivarParticipanteManejador(
        IAutorizadorUsuarioActivo autorizador,
        IRepositorioParticipantes repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IRegistroLogsAplicacion registroLogs)
    {
        _autorizador = autorizador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _registroLogs = registroLogs;
    }

    public async Task<CambiarEstadoUsuarioRespuestaDto> Handle(
        DesactivarParticipanteComando comando, CancellationToken cancelacion)
    {
        var invocador = await _autorizador.RequerirRolesActivosAsync(
            new[] { RolUsuario.Administrador, RolUsuario.Operador }, cancelacion);

        var participante = await _repositorio.ObtenerPorIdAsync(comando.IdParticipante, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un Participante con id {comando.IdParticipante}.");

        if (participante.Estado != EstadoUsuario.Activo)
        {
            _registroLogs.Advertencia(
                evento: "ParticipanteYaInactivo",
                descripcion: "Solicitud de desactivación sobre un Participante que ya está Inactivo.",
                propiedades: new Dictionary<string, object?>
                {
                    ["ParticipanteId"] = participante.Id,
                    ["ActorId"] = invocador.Id
                });
            throw new UsuarioYaInactivoExcepcion();
        }

        participante.Desactivar();

        await _repositorio.ActualizarEstadoAsync(participante, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registroLogs.Informacion(
            evento: "ParticipanteDesactivado",
            descripcion: "Usuario desactivó un participante correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["ParticipanteId"] = participante.Id,
                ["ActorId"] = invocador.Id
            });

        return new CambiarEstadoUsuarioRespuestaDto
        {
            IdUsuario = participante.Id,
            Estado = participante.Estado.ToString(),
            Mensaje = "Participante desactivado correctamente."
        };
    }
}
