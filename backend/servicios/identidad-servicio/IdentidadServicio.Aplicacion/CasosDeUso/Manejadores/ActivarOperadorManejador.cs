using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class ActivarOperadorManejador
    : IRequestHandler<ActivarOperadorComando, CambiarEstadoUsuarioRespuestaDto>
{
    private readonly IAutorizadorUsuarioActivo _autorizador;
    private readonly IRepositorioOperadores _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly ILogger<ActivarOperadorManejador> _registro;

    public ActivarOperadorManejador(
        IAutorizadorUsuarioActivo autorizador,
        IRepositorioOperadores repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        ILogger<ActivarOperadorManejador> registro)
    {
        _autorizador = autorizador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _registro = registro;
    }

    public async Task<CambiarEstadoUsuarioRespuestaDto> Handle(
        ActivarOperadorComando comando, CancellationToken cancelacion)
    {
        await _autorizador.RequerirRolActivoAsync(RolUsuario.Administrador, cancelacion);
        var operador = await _repositorio.ObtenerPorIdAsync(comando.IdOperador, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un Operador con id {comando.IdOperador}.");

        if (operador.Estado == EstadoUsuario.Activo)
        {
            _registro.LogInformation(
                "Solicitud de activación sobre Operador {Id} ya Activo.", operador.Id);
            throw new UsuarioYaActivoExcepcion();
        }

        operador.Activar();

        await _repositorio.ActualizarEstadoAsync(operador, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "Operador {Id} activado por Administrador.", operador.Id);

        return new CambiarEstadoUsuarioRespuestaDto
        {
            IdUsuario = operador.Id,
            Estado = operador.Estado.ToString(),
            Mensaje = "Operador activado correctamente."
        };
    }
}
