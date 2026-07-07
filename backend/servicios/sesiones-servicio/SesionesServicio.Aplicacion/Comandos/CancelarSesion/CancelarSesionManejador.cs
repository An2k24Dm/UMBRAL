using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.CancelarSesion;

public sealed class CancelarSesionManejador : IRequestHandler<CancelarSesionComando>
{
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public CancelarSesionManejador(
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _registroLogs = registroLogs;
    }

    public async Task Handle(CancelarSesionComando comando, CancellationToken cancelacion)
    {
        if (!_usuarioActual.EstaAutenticado())
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para realizar esta acción.");

        if (!_usuarioActual.TieneAlgunRol(RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Solo un Operador puede cancelar sesiones.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        var operadorId = _usuarioActual.ObtenerId() ?? Guid.Empty;
        if (sesion.OperadorCreadorId != operadorId)
            throw new AccesoSesionNoPermitidoExcepcion(
                "No tiene permiso para cancelar esta sesión.");

        sesion.Cancelar();

        await _repositorio.ActualizarAsync(sesion, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _registroLogs.Informacion(
            evento: "SesionCancelada",
            descripcion: "Operador canceló la sesión",
            propiedades: new Dictionary<string, object?>
            {
                ["SesionId"] = sesion.Id,
                ["OperadorId"] = operadorId
            });
    }
}
