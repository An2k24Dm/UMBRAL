using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Aplicacion.Validaciones.OperacionSesion;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Excepciones;
using SesionesServicio.Dominio.ObjetosValor;

namespace SesionesServicio.Aplicacion.Fachadas;

public sealed class FachadaOperacionSesion : IFachadaOperacionSesion
{
    private const string RolOperador = "Operador";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly INotificadorSesionesTiempoReal _notificador;
    private readonly IClienteJuegosMisiones _clienteMisiones;
    private readonly IProveedorFechaHora _reloj;
    private readonly IRegistroLogsAplicacion _registroLogs;
    private readonly ValidadorInicioSesionOperacion _validadorInicio;
    private readonly ValidadorCancelacionSesionOperacion _validadorCancelacion;

    public FachadaOperacionSesion(
        IRepositorioSesiones repositorio,
        IUnidadTrabajoSesiones unidadTrabajo,
        IUsuarioActual usuarioActual,
        INotificadorSesionesTiempoReal notificador,
        IClienteJuegosMisiones clienteMisiones,
        IProveedorFechaHora reloj,
        IRegistroLogsAplicacion registroLogs,
        ValidadorInicioSesionOperacion validadorInicio,
        ValidadorCancelacionSesionOperacion validadorCancelacion)
    {
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _notificador = notificador;
        _clienteMisiones = clienteMisiones;
        _reloj = reloj;
        _registroLogs = registroLogs;
        _validadorInicio = validadorInicio;
        _validadorCancelacion = validadorCancelacion;
    }

    public async Task<OperacionSesionRespuestaDto> IniciarAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var operadorId = AutorizarOperador();
        var sesion = await ObtenerSesionPropiaAsync(sesionId, operadorId, cancelacion);
        var estadoAnterior = sesion.Estado;
        var ahoraUtc = _reloj.ObtenerFechaHoraUtc();
        _validadorInicio.Validar(sesion, ahoraUtc);
        var secuencia = await ConstruirSecuenciaCompletaAsync(sesion, cancelacion);
        sesion.EstablecerSecuenciaEtapas(secuencia);
        var primeraEtapa = secuencia[0];
        if (sesion.Estado == EstadoSesion.Programada)
            sesion.Preparar();
        sesion.IniciarPrimeraEtapa(primeraEtapa, ahoraUtc);
        await GuardarYNotificarAsync(sesion, cancelacion);
        await _notificador.NotificarEtapaIniciadaAsync(
            sesion.Id,
            primeraEtapa.MisionId,
            primeraEtapa.EtapaId,
            primeraEtapa.TipoEtapa,
            primeraEtapa.ModoDeJuegoId,
            primeraEtapa.OrdenGlobal,
            ahoraUtc,
            primeraEtapa.DuracionSegundos,
            cancelacion);
        Registrar("SesionIniciada", "Operador inició la sesión correctamente",
            sesion, operadorId, estadoAnterior, ahoraUtc);

        return Construir(sesion, "Sesión iniciada correctamente.");
    }

    public async Task<OperacionSesionRespuestaDto> PausarAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var operadorId = AutorizarOperador();
        var sesion = await ObtenerSesionPropiaAsync(sesionId, operadorId, cancelacion);
        var estadoAnterior = sesion.Estado;
        sesion.Pausar(_reloj.ObtenerFechaHoraUtc());
        await GuardarYNotificarAsync(sesion, cancelacion);
        Registrar("SesionPausada", "Operador pausó la sesión correctamente",
            sesion, operadorId, estadoAnterior, _reloj.ObtenerFechaHoraUtc());

        return Construir(sesion, "Sesión pausada correctamente.");
    }

    public async Task<OperacionSesionRespuestaDto> ReanudarAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var operadorId = AutorizarOperador();
        var sesion = await ObtenerSesionPropiaAsync(sesionId, operadorId, cancelacion);
        var estadoAnterior = sesion.Estado;
        sesion.Reanudar(_reloj.ObtenerFechaHoraUtc());
        await GuardarYNotificarAsync(sesion, cancelacion);
        Registrar("SesionReanudada", "Operador reanudó la sesión correctamente",
            sesion, operadorId, estadoAnterior, _reloj.ObtenerFechaHoraUtc());
        return Construir(sesion, "Sesión reanudada correctamente.");
    }

    public async Task<OperacionSesionRespuestaDto> CancelarAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var operadorId = AutorizarOperador();
        var sesion = await ObtenerSesionPropiaAsync(sesionId, operadorId, cancelacion);
        var estadoAnterior = sesion.Estado;
        _validadorCancelacion.Validar(sesion);
        sesion.Cancelar();
        await GuardarYNotificarAsync(sesion, cancelacion);
        Registrar("SesionCancelada", "Operador canceló la sesión correctamente",
            sesion, operadorId, estadoAnterior, _reloj.ObtenerFechaHoraUtc());

        return Construir(sesion, "Sesión cancelada correctamente.");
    }

    public Task<OperacionSesionRespuestaDto?> FinalizarSiCorrespondeAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        return Task.FromResult<OperacionSesionRespuestaDto?>(null);
    }

    private Guid AutorizarOperador()
    {
        if (!_usuarioActual.EstaAutenticado())
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Debe iniciar sesión para operar una sesión.");

        if (!_usuarioActual.TieneAlgunRol(RolOperador))
            throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "Solo un Operador puede operar sesiones.");

        return _usuarioActual.ObtenerId()
            ?? throw new UsuarioNoAutorizadoCrearSesionExcepcion(
                "No se pudo determinar la identidad del operador.");
    }

    private async Task<Sesion> ObtenerSesionPropiaAsync(
        Guid sesionId, Guid operadorId, CancellationToken cancelacion)
    {
        var sesion = await _repositorio.ObtenerPorIdAsync(sesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        if (sesion.OperadorCreadorId != operadorId)
            throw new AccesoSesionNoPermitidoExcepcion(
                "No tiene permiso para operar esta sesión.");

        return sesion;
    }

    private async Task GuardarYNotificarAsync(Sesion sesion, CancellationToken cancelacion)
    {
        await _repositorio.ActualizarAsync(sesion, cancelacion);
        await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        await _notificador.NotificarSesionActualizadaAsync(
            sesion.Id, sesion.Estado.ToString(), cancelacion);
    }

    private void Registrar(
        string evento,
        string descripcion,
        Sesion sesion,
        Guid operadorId,
        EstadoSesion estadoAnterior,
        DateTime fechaEventoUtc)
    {
        _registroLogs.Informacion(
            evento: evento,
            descripcion: descripcion,
            propiedades: new Dictionary<string, object?>
            {
                ["SesionId"] = sesion.Id,
                ["OperadorId"] = operadorId,
                ["EstadoAnterior"] = estadoAnterior.ToString(),
                ["EstadoNuevo"] = sesion.Estado.ToString(),
                ["FechaEventoUtc"] = fechaEventoUtc
            });
    }

    private static OperacionSesionRespuestaDto Construir(Sesion sesion, string mensaje)
        => new()
        {
            SesionId = sesion.Id,
            Estado = sesion.Estado.ToString(),
            FechaInicioUtc = sesion.FechaInicioUtc,
            FechaFinalizacionUtc = sesion.FechaFinalizacionUtc,
            Mensaje = mensaje
        };

    private async Task<List<EjecucionActualSesion>> ConstruirSecuenciaCompletaAsync(
        Sesion sesion, CancellationToken cancelacion)
    {
        if (sesion.Misiones.Count == 0)
            throw new MisionSinEtapasExcepcion(
                "La sesion no tiene misiones asignadas para iniciar.");

        var secuencia = new List<EjecucionActualSesion>();
        var ordenGlobal = 1;

        foreach (var sesionMision in sesion.Misiones.OrderBy(m => m.Orden))
        {
            var mision = await _clienteMisiones.ObtenerMisionConEtapasAsync(
                sesionMision.MisionId, cancelacion)
                ?? throw new MisionNoEncontradaExcepcion(
                    "La mision asociada a la sesion no existe.");

            var ordenEtapa = 1;
            foreach (var etapa in mision.Etapas.OrderBy(e => e.Orden))
            {
                secuencia.Add(EjecucionActualSesion.Planificar(
                    sesionMision.MisionId,
                    etapa.Id,
                    etapa.ModoDeJuegoId,
                    etapa.TipoModoDeJuego,
                    ordenGlobal++,
                    sesionMision.Orden,
                    ordenEtapa++,
                    etapa.TiempoEstimado));
            }
        }

        if (secuencia.Count == 0)
            throw new MisionSinEtapasExcepcion(
                "Las misiones de la sesion no contienen etapas para iniciar.");

        return secuencia;
    }
}
