using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Dominio.Abstract;
using PartidasServicio.Dominio.Entidades;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.Aplicacion.Servicios;

public sealed class ServicioPartidas : IServicioPartidas
{
    private readonly IRepositorioPartidas _repositorio;
    private readonly IUnidadTrabajoPartidas _unidadTrabajo;
    private readonly INotificadorPartidasTiempoReal _notificador;
    private readonly IProveedorFechaHora _reloj;
    private readonly IRegistroLogsAplicacion _logs;

    public ServicioPartidas(
        IRepositorioPartidas repositorio,
        IUnidadTrabajoPartidas unidadTrabajo,
        INotificadorPartidasTiempoReal notificador,
        IProveedorFechaHora reloj,
        IRegistroLogsAplicacion logs)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _notificador = notificador;
        _reloj = reloj;
        _logs = logs;
    }

    public async Task IniciarPartidaAsync(Guid sesionId, CancellationToken cancelacion = default)
    {
        ValidarSesionId(sesionId);

        var partida = await _repositorio.ObtenerPorSesionIdAsync(sesionId, cancelacion)
            ?? Partida.Crear(sesionId);

        partida.Iniciar(_reloj.ObtenerFechaHoraUtc());
        await _repositorio.GuardarAsync(partida, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _logs.Informacion("PartidaIniciada", "Partida iniciada.", new Dictionary<string, object?> { ["SesionId"] = sesionId });
        await _notificador.NotificarCambioEstadoPartidaAsync(sesionId, "Iniciada", cancelacion);
    }

    public async Task PausarPartidaAsync(Guid sesionId, CancellationToken cancelacion = default)
    {
        ValidarSesionId(sesionId);

        var partida = await ObtenerPartidaOFallarAsync(sesionId, cancelacion);
        partida.Pausar();
        await _repositorio.GuardarAsync(partida, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _logs.Informacion("PartidaPausada", "Partida pausada.", new Dictionary<string, object?> { ["SesionId"] = sesionId });
        await _notificador.NotificarCambioEstadoPartidaAsync(sesionId, "Pausada", cancelacion);
    }

    public async Task ReanudarPartidaAsync(Guid sesionId, CancellationToken cancelacion = default)
    {
        ValidarSesionId(sesionId);

        var partida = await ObtenerPartidaOFallarAsync(sesionId, cancelacion);
        partida.Reanudar();
        await _repositorio.GuardarAsync(partida, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _logs.Informacion("PartidaReanudada", "Partida reanudada.", new Dictionary<string, object?> { ["SesionId"] = sesionId });
        await _notificador.NotificarCambioEstadoPartidaAsync(sesionId, "Iniciada", cancelacion);
    }

    public async Task FinalizarPartidaAsync(Guid sesionId, CancellationToken cancelacion = default)
    {
        ValidarSesionId(sesionId);

        var partida = await ObtenerPartidaOFallarAsync(sesionId, cancelacion);
        partida.Finalizar(_reloj.ObtenerFechaHoraUtc());
        await _repositorio.GuardarAsync(partida, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _logs.Informacion("PartidaFinalizada", "Partida finalizada.", new Dictionary<string, object?> { ["SesionId"] = sesionId });
        await _notificador.NotificarCambioEstadoPartidaAsync(sesionId, "Finalizada", cancelacion);
    }

    public async Task CancelarPartidaAsync(Guid sesionId, CancellationToken cancelacion = default)
    {
        ValidarSesionId(sesionId);

        var partida = await ObtenerPartidaOFallarAsync(sesionId, cancelacion);
        partida.Cancelar(_reloj.ObtenerFechaHoraUtc());
        await _repositorio.GuardarAsync(partida, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);

        _logs.Informacion("PartidaCancelada", "Partida cancelada.", new Dictionary<string, object?> { ["SesionId"] = sesionId });
        await _notificador.NotificarCambioEstadoPartidaAsync(sesionId, "Cancelada", cancelacion);
    }

    private async Task<Partida> ObtenerPartidaOFallarAsync(Guid sesionId, CancellationToken cancelacion)
        => await _repositorio.ObtenerPorSesionIdAsync(sesionId, cancelacion)
           ?? throw new PartidaNoEncontradaExcepcion(sesionId);

    private static void ValidarSesionId(Guid sesionId)
    {
        if (sesionId == Guid.Empty)
            throw new ExcepcionDominio("El identificador de la sesión es obligatorio.");
    }
}
