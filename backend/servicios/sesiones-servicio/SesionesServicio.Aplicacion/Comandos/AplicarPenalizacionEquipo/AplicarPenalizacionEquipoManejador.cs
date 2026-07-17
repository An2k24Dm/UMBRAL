using MediatR;
using SesionesServicio.Aplicacion.Comandos.Penalizaciones;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionEquipo;

public sealed class AplicarPenalizacionEquipoManejador
    : IRequestHandler<AplicarPenalizacionEquipoComando, PenalizacionEncoladaDto>
{
    private const string RolOperador = "Operador";
    private const string TipoObjetivoEquipo = "Equipo";
    private const string EstadoPendiente = "Pendiente";

    private readonly IRepositorioSesiones _repositorio;
    private readonly IRepositorioPenalizacionesSesion _repositorioPenalizaciones;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IPublicadorEventosRanking _publicador;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorFechaHora _reloj;
    private readonly IValidador<AplicarPenalizacionEquipoComando> _validador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public AplicarPenalizacionEquipoManejador(
        IRepositorioSesiones repositorio,
        IRepositorioPenalizacionesSesion repositorioPenalizaciones,
        IUnidadTrabajoSesiones unidadTrabajo,
        IPublicadorEventosRanking publicador,
        IUsuarioActual usuarioActual,
        IProveedorFechaHora reloj,
        IValidador<AplicarPenalizacionEquipoComando> validador,
        IRegistroLogsAplicacion registroLogs)
    {
        _repositorio = repositorio;
        _repositorioPenalizaciones = repositorioPenalizaciones;
        _unidadTrabajo = unidadTrabajo;
        _publicador = publicador;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
        _validador = validador;
        _registroLogs = registroLogs;
    }

    public async Task<PenalizacionEncoladaDto> Handle(
        AplicarPenalizacionEquipoComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        if (!_usuarioActual.EstaAutenticado())
            throw new AccesoSesionNoPermitidoExcepcion(
                "Debe iniciar sesión para aplicar penalizaciones.");

        if (!_usuarioActual.TieneAlgunRol(RolOperador))
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo un Operador puede aplicar penalizaciones.");

        var operadorId = _usuarioActual.ObtenerId()
            ?? throw new AccesoSesionNoPermitidoExcepcion(
                "No se pudo determinar la identidad del operador.");

        var sesion = await _repositorio.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new SesionNoEncontradaExcepcion("La sesión solicitada no existe.");

        if (sesion is not SesionGrupal grupal)
            throw new SesionInvalidaExcepcion(
                "Este endpoint solo aplica a sesiones grupales. " +
                "Use el endpoint de participantes para sesiones individuales.");

        if (sesion.OperadorCreadorId != operadorId)
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo el Operador creador de la sesión puede aplicar penalizaciones.");

        var equipo = grupal.ObtenerEquipoParaPenalizar(comando.EquipoId);

        var eventoId = Guid.NewGuid();
        var aplicadaEnUtc = _reloj.ObtenerFechaHoraUtc();
        var penalizacion = PenalizacionSesion.CrearParaEquipo(
            eventoId,
            sesion.Id,
            equipo.Id,
            comando.Puntos,
            comando.Motivo ?? string.Empty,
            operadorId,
            aplicadaEnUtc);

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            await _repositorioPenalizaciones.AgregarAsync(penalizacion, ct);
            await _publicador.PublicarPenalizacionAplicadaAsync(
                eventoId,
                penalizacion.Id,
                sesion.Id,
                TipoObjetivoEquipo,
                null,
                null,
                equipo.Id,
                penalizacion.Puntos,
                penalizacion.Motivo,
                operadorId,
                aplicadaEnUtc,
                ct);
        }, cancelacion);

        _registroLogs.Informacion(
            evento: "PenalizacionEquipoRegistrada",
            descripcion: "Operador registró una penalización de equipo y la encoló hacia Ranking",
            propiedades: new Dictionary<string, object?>
            {
                ["PenalizacionId"] = penalizacion.Id,
                ["EventoId"] = eventoId,
                ["SesionId"] = sesion.Id,
                ["TipoObjetivo"] = TipoObjetivoEquipo,
                ["ObjetivoId"] = equipo.Id,
                ["Puntos"] = penalizacion.Puntos,
                ["OperadorIdentidadId"] = operadorId,
                ["Estado"] = EstadoPendiente
            });

        return new PenalizacionEncoladaDto(penalizacion.Id, eventoId, EstadoPendiente);
    }
}
