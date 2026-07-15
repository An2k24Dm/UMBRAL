using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.ActivarParticipante;

public sealed class ActivarParticipanteManejador
    : IRequestHandler<ActivarParticipanteComando, CambiarEstadoUsuarioRespuestaDto>
{
    private readonly IAutorizadorUsuarioActivo _autorizador;
    private readonly IRepositorioParticipantes _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ActivarParticipanteManejador(
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
        ActivarParticipanteComando comando, CancellationToken cancelacion)
    {
        var invocador = await _autorizador.RequerirRolesActivosAsync(
            new[] { RolUsuario.Administrador, RolUsuario.Operador }, cancelacion);

        var participante = await _repositorio.ObtenerPorIdAsync(comando.IdParticipante, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un Participante con id {comando.IdParticipante}.");

        if (participante.Estado == EstadoUsuario.Activo)
        {
            _registroLogs.Advertencia(
                evento: "ParticipanteYaActivo",
                descripcion: "Solicitud de activación sobre un Participante que ya está Activo.",
                propiedades: new Dictionary<string, object?>
                {
                    ["ParticipanteId"] = participante.Id,
                    ["ActorId"] = invocador.Id
                });
            throw new UsuarioYaActivoExcepcion();
        }

        participante.Activar();

        await _repositorio.ActualizarEstadoAsync(participante, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registroLogs.Informacion(
            evento: "ParticipanteActivado",
            descripcion: "Usuario activó un participante correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["ParticipanteId"] = participante.Id,
                ["ActorId"] = invocador.Id
            });

        return new CambiarEstadoUsuarioRespuestaDto
        {
            IdUsuario = participante.Id,
            Estado = participante.Estado.ToString(),
            Mensaje = "Participante activado correctamente."
        };
    }
}
