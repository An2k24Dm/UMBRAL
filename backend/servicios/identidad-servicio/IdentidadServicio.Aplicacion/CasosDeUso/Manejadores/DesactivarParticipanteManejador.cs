using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class DesactivarParticipanteManejador
    : IRequestHandler<DesactivarParticipanteComando, CambiarEstadoUsuarioRespuestaDto>
{
    private readonly IAutorizadorUsuarioActivo _autorizador;
    private readonly IRepositorioParticipantes _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly ILogger<DesactivarParticipanteManejador> _registro;

    public DesactivarParticipanteManejador(
        IAutorizadorUsuarioActivo autorizador,
        IRepositorioParticipantes repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        ILogger<DesactivarParticipanteManejador> registro)
    {
        _autorizador = autorizador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _registro = registro;
    }

    public async Task<CambiarEstadoUsuarioRespuestaDto> Handle(
        DesactivarParticipanteComando comando, CancellationToken cancelacion)
    {
        // 1) Invocador: Administrador o Operador, ambos Activos.
        var invocador = await _autorizador.RequerirRolesActivosAsync(
            new[] { RolUsuario.Administrador, RolUsuario.Operador }, cancelacion);

        // 2) Resolver el Participante. ObtenerPorIdAsync filtra por rol
        //    Participante; devuelve null si el id es de otro rol.
        var participante = await _repositorio.ObtenerPorIdAsync(comando.IdParticipante, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un Participante con id {comando.IdParticipante}.");

        // 3) Idempotencia controlada: ya Inactivo → 400 controlado.
        if (participante.Estado != EstadoUsuario.Activo)
        {
            _registro.LogInformation(
                "Solicitud HU12 sobre Participante {Id} ya Inactivo (invocador {Invocador}).",
                participante.Id, invocador.Id);
            throw new UsuarioYaInactivoExcepcion();
        }

        // 4) Regla de dominio.
        participante.Desactivar();

        // 5) Persistir SÓLO el Estado.
        await _repositorio.ActualizarEstadoAsync(participante, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "Participante {Id} desactivado por usuario {Invocador} (HU12).",
            participante.Id, invocador.Id);

        return new CambiarEstadoUsuarioRespuestaDto
        {
            IdUsuario = participante.Id,
            Estado = participante.Estado.ToString(),
            Mensaje = "Participante desactivado correctamente."
        };
    }
}
