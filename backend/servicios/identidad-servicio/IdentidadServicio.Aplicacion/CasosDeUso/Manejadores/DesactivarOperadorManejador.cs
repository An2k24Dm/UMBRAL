using IdentidadServicio.Aplicacion.CasosDeUso.Comandos;
using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Aplicacion.Servicios.Usuarios;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IdentidadServicio.Aplicacion.CasosDeUso.Manejadores;

public sealed class DesactivarOperadorManejador
    : IRequestHandler<DesactivarOperadorComando, CambiarEstadoUsuarioRespuestaDto>
{
    private readonly IAutorizadorUsuarioActivo _autorizador;
    private readonly IRepositorioOperadores _repositorio;
    private readonly IUnidadTrabajoIdentidad _unidadTrabajo;
    private readonly ILogger<DesactivarOperadorManejador> _registro;

    public DesactivarOperadorManejador(
        IAutorizadorUsuarioActivo autorizador,
        IRepositorioOperadores repositorio,
        IUnidadTrabajoIdentidad unidadTrabajo,
        ILogger<DesactivarOperadorManejador> registro)
    {
        _autorizador = autorizador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _registro = registro;
    }

    public async Task<CambiarEstadoUsuarioRespuestaDto> Handle(
        DesactivarOperadorComando comando, CancellationToken cancelacion)
    {
        // 1) Invocador: sólo un Administrador Activo puede desactivar Operadores.
        await _autorizador.RequerirRolActivoAsync(RolUsuario.Administrador, cancelacion);

        // 2) Resolver el objetivo. ObtenerPorIdAsync filtra por rol Operador,
        //    así que devuelve null si el id pertenece a otro rol.
        var operador = await _repositorio.ObtenerPorIdAsync(comando.IdOperador, cancelacion)
            ?? throw new DatosUsuarioInvalidosExcepcion(
                $"No existe un Operador con id {comando.IdOperador}.");

        // 3) Idempotencia controlada: si ya está Inactivo, error de negocio.
        if (operador.Estado != EstadoUsuario.Activo)
        {
            _registro.LogInformation(
                "Solicitud HU12 sobre Operador {Id} ya Inactivo.", operador.Id);
            throw new UsuarioYaInactivoExcepcion();
        }

        // 4) Regla de dominio: mover a Inactivo.
        operador.Desactivar();

        // 5) Persistir SÓLO el Estado.
        await _repositorio.ActualizarEstadoAsync(operador, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registro.LogInformation(
            "Operador {Id} desactivado por Administrador (HU12).", operador.Id);

        return new CambiarEstadoUsuarioRespuestaDto
        {
            IdUsuario = operador.Id,
            Estado = operador.Estado.ToString(),
            Mensaje = "Operador desactivado correctamente."
        };
    }
}
