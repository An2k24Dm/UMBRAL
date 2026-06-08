using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ActivarParticipanteManejador
    : IRequestHandler<ActivarParticipanteComando, CambiarEstadoUsuarioRespuestaDto>
{
    private readonly IAutorizadorUsuarioActivo _autorizador;
    private readonly IRepositorioParticipantes _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly ILogger<ActivarParticipanteManejador> _registro;

    public ActivarParticipanteManejador(
        IAutorizadorUsuarioActivo autorizador,
        IRepositorioParticipantes repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        ILogger<ActivarParticipanteManejador> registro)
    {
        _autorizador = autorizador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _registro = registro;
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
            _registro.LogInformation(
                "Solicitud de activación sobre Participante {Id} ya Activo (invocador {Invocador}).",
                participante.Id, invocador.Id);
            throw new UsuarioYaActivoExcepcion();
        }

        participante.Activar();

        await _repositorio.ActualizarEstadoAsync(participante, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "Participante {Id} activado por usuario {Invocador}.",
            participante.Id, invocador.Id);

        return new CambiarEstadoUsuarioRespuestaDto
        {
            IdUsuario = participante.Id,
            Estado = participante.Estado.ToString(),
            Mensaje = "Participante activado correctamente."
        };
    }
}
