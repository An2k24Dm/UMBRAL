using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos.Penalizaciones;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Eventos;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Comandos.AplicarPenalizacionParticipante;

public sealed class AplicarPenalizacionParticipanteManejador
    : IRequestHandler<AplicarPenalizacionParticipanteComando, PenalizacionEncoladaDto>
{
    private const string RolOperador = "Operador";
    private const string TipoObjetivoParticipante = "Participante";
    private const string EstadoPendiente = "Pendiente";
    private readonly IRepositorioSesiones _repositorio;
    private readonly IRepositorioPenalizacionesAplicadas _repositorioPenalizaciones;
    private readonly IUnidadTrabajoSesiones _unidadTrabajo;
    private readonly IPublicadorEventosRanking _publicador;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorFechaHora _reloj;
    private readonly IValidador<AplicarPenalizacionParticipanteComando> _validador;
    private readonly IRegistroLogsAplicacion _registroLogs;

    public AplicarPenalizacionParticipanteManejador(
        IRepositorioSesiones repositorio,
        IRepositorioPenalizacionesAplicadas repositorioPenalizaciones,
        IUnidadTrabajoSesiones unidadTrabajo,
        IPublicadorEventosRanking publicador,
        IUsuarioActual usuarioActual,
        IProveedorFechaHora reloj,
        IValidador<AplicarPenalizacionParticipanteComando> validador,
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
        AplicarPenalizacionParticipanteComando comando, CancellationToken cancelacion)
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

        if (sesion is not SesionIndividual individual)
            throw new SesionInvalidaExcepcion(
                "Este endpoint solo aplica a sesiones individuales. " +
                "Use el endpoint de equipos para sesiones grupales.");

        if (sesion.OperadorCreadorId != operadorId)
            throw new AccesoSesionNoPermitidoExcepcion(
                "Solo el Operador creador de la sesión puede aplicar penalizaciones.");

        // Valida estado (Activa/Pausada) y que el participante pertenezca a la sesión.
        var participante = individual.ObtenerParticipanteParaPenalizar(comando.ParticipanteSesionId);

        var eventoId = Guid.NewGuid();
        var aplicadaEnUtc = _reloj.ObtenerFechaHoraUtc();
        var penalizacion = PenalizacionAplicada.CrearParaParticipante(
            eventoId,
            sesion.Id,
            participante.Id,
            participante.ParticipanteIdentidadId,
            comando.Puntos,
            comando.Motivo ?? string.Empty,
            operadorId,
            aplicadaEnUtc);

        await _unidadTrabajo.EjecutarEnTransaccionAsync(async ct =>
        {
            await _repositorioPenalizaciones.AgregarAsync(penalizacion, ct);
            await _publicador.PublicarPenalizacionAplicadaAsync(
                eventoId,
                sesion.Id,
                TipoObjetivoParticipante,
                participante.Id,
                participante.ParticipanteIdentidadId,
                null,
                penalizacion.PuntosDescontados,
                penalizacion.Motivo,
                operadorId,
                aplicadaEnUtc,
                ct);
            await _unidadTrabajo.GuardarCambiosAsync(ct);
        }, cancelacion);

        _registroLogs.Informacion(
            evento: "PenalizacionParticipanteRegistrada",
            descripcion: "Operador registró una penalización de participante y la encoló hacia Ranking",
            propiedades: new Dictionary<string, object?>
            {
                ["EventoId"] = eventoId,
                ["SesionId"] = sesion.Id,
                ["TipoObjetivo"] = TipoObjetivoParticipante,
                ["ObjetivoId"] = participante.Id,
                ["Puntos"] = penalizacion.PuntosDescontados,
                ["OperadorIdentidadId"] = operadorId,
                ["Estado"] = EstadoPendiente
            });

        return new PenalizacionEncoladaDto(eventoId, EstadoPendiente);
    }
}
