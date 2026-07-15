using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;

namespace IdentidadServicio.Aplicacion.Comandos.ActivarOperador;

public sealed class ActivarOperadorManejador
    : IRequestHandler<ActivarOperadorComando, CambiarEstadoUsuarioRespuestaDto>
{
    private readonly IAutorizadorUsuarioActivo _autorizador;
    private readonly IRepositorioOperadores _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public ActivarOperadorManejador(
        IAutorizadorUsuarioActivo autorizador,
        IRepositorioOperadores repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        IRegistroLogsAplicacion registroLogs)
    {
        _autorizador = autorizador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _registroLogs = registroLogs;
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
            _registroLogs.Advertencia(
                evento: "OperadorYaActivo",
                descripcion: "Solicitud de activación sobre un Operador que ya está Activo.",
                propiedades: new Dictionary<string, object?>
                {
                    ["OperadorId"] = operador.Id
                });
            throw new UsuarioYaActivoExcepcion();
        }

        operador.Activar();

        await _repositorio.ActualizarEstadoAsync(operador, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registroLogs.Informacion(
            evento: "OperadorActivado",
            descripcion: "Administrador activó un operador correctamente",
            propiedades: new Dictionary<string, object?>
            {
                ["OperadorId"] = operador.Id
            });

        return new CambiarEstadoUsuarioRespuestaDto
        {
            IdUsuario = operador.Id,
            Estado = operador.Estado.ToString(),
            Mensaje = "Operador activado correctamente."
        };
    }
}
