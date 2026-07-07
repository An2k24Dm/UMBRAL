using MediatR;
using PartidasServicio.Aplicacion.Cadena;
using PartidasServicio.Aplicacion.Estrategias;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Aplicacion.Validaciones;
using PartidasServicio.Commons.Dtos;
using PartidasServicio.Dominio.Abstract;
using PartidasServicio.Dominio.Entidades;
using PartidasServicio.Dominio.Excepciones;

namespace PartidasServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;

public sealed class EnviarRespuestaTriviaManejador
    : IRequestHandler<EnviarRespuestaTriviaComando, RespuestaTriviaResultadoDto>
{
    private readonly IValidador<EnviarRespuestaTriviaComando> _validador;
    private readonly IRepositorioRespuestas _repositorio;
    private readonly IUnidadTrabajoPartidas _unidadTrabajo;
    private readonly IUsuarioActual _usuarioActual;
    private readonly IProveedorFechaHora _reloj;
    private readonly IClienteJuegos _clienteJuegos;
    private readonly IConsultasPartidas _consultas;
    private readonly IClienteSesiones _clienteSesiones;
    private readonly INotificadorPartidasTiempoReal _notificador;
    private readonly ICalculadoraPuntaje _calculadora;
    private readonly IRegistroLogsAplicacion _logs;

    // Cadena: EstadoPartida → EstadoSesion → Participante → Concurrencia
    private readonly IReadOnlyList<IEslabonValidacion> _cadena;

    public EnviarRespuestaTriviaManejador(
        IValidador<EnviarRespuestaTriviaComando> validador,
        IRepositorioRespuestas repositorio,
        IUnidadTrabajoPartidas unidadTrabajo,
        IUsuarioActual usuarioActual,
        IProveedorFechaHora reloj,
        IClienteJuegos clienteJuegos,
        IConsultasPartidas consultas,
        IClienteSesiones clienteSesiones,
        INotificadorPartidasTiempoReal notificador,
        ICalculadoraPuntaje calculadora,
        IRegistroLogsAplicacion logs,
        EslabonEstadoPartida eslabonEstadoPartida,
        EslabonEstadoSesion eslabonEstado,
        EslabonParticipanteEnSesion eslabonParticipante,
        EslabonConcurrencia eslabonConcurrencia)
    {
        _validador = validador;
        _repositorio = repositorio;
        _unidadTrabajo = unidadTrabajo;
        _usuarioActual = usuarioActual;
        _reloj = reloj;
        _clienteJuegos = clienteJuegos;
        _consultas = consultas;
        _notificador = notificador;
        _clienteSesiones = clienteSesiones;
        _calculadora = calculadora;
        _logs = logs;
        _cadena = [eslabonEstado, eslabonEstadoPartida, eslabonParticipante, eslabonConcurrencia];
    }

    public async Task<RespuestaTriviaResultadoDto> Handle(
        EnviarRespuestaTriviaComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        var participanteId = _usuarioActual.ObtenerId()
            ?? throw new ExcepcionDominio("No se pudo obtener el participante autenticado.");

        var contexto = new ContextoValidacionRespuesta
        {
            SesionId = comando.SesionId,
            PreguntaId = comando.Dto.PreguntaId,
            ParticipanteId = participanteId
        };

        foreach (var eslabon in _cadena)
            await eslabon.ValidarAsync(contexto, cancelacion);

        if (contexto.PreguntaYaRespondida)
        {
            _logs.Informacion("PreguntaYaRespondida", "El equipo ya respondió esta pregunta.",
                new Dictionary<string, object?> { ["SesionId"] = comando.SesionId, ["PreguntaId"] = comando.Dto.PreguntaId });

            return new RespuestaTriviaResultadoDto
            {
                YaRespondida = true, EsCorrecta = false, PuntosGanados = 0,
                Mensaje = "Tu equipo ya respondió esta pregunta."
            };
        }

        var verificacion = await _clienteJuegos.VerificarRespuestaAsync(
            comando.TriviaId, comando.Dto.PreguntaId, comando.Dto.OpcionSeleccionadaId, cancelacion)
            ?? throw new PreguntaNoEncontradaExcepcion(comando.Dto.PreguntaId);

        var puntosGanados = verificacion.EsCorrecta
            ? _calculadora.Calcular(verificacion.PuntajeBase, comando.Dto.TiempoTardadoMs, verificacion.TiempoLimiteMs)
            : 0;

        var respuesta = RespuestaTrivia.Crear(
            sesionId: comando.SesionId,
            misionId: comando.MisionId,
            etapaId: comando.EtapaId,
            preguntaId: comando.Dto.PreguntaId,
            opcionSeleccionadaId: comando.Dto.OpcionSeleccionadaId,
            participanteId: participanteId,
            equipoId: contexto.EquipoId,
            esCorrecta: verificacion.EsCorrecta,
            puntosGanados: puntosGanados,
            tiempoTardadoMs: comando.Dto.TiempoTardadoMs,
            fechaRespuestaUtc: _reloj.ObtenerFechaHoraUtc());

        try
        {
            await _repositorio.AgregarAsync(respuesta, cancelacion);
            await _unidadTrabajo.GuardarCambiosAsync(cancelacion);
        }
        catch (RespuestaDuplicadaExcepcion)
        {
            _logs.Advertencia("ConcurrenciaRespuestaTrivia", "Colisión de concurrencia al guardar respuesta.",
                new Dictionary<string, object?> { ["SesionId"] = comando.SesionId, ["EquipoId"] = contexto.EquipoId });

            return new RespuestaTriviaResultadoDto
            {
                YaRespondida = true, EsCorrecta = false, PuntosGanados = 0,
                Mensaje = "Tu equipo ya respondió esta pregunta."
            };
        }

        _logs.Informacion("RespuestaTriviaRegistrada", "Respuesta registrada.",
            new Dictionary<string, object?> { ["SesionId"] = comando.SesionId, ["EsCorrecta"] = verificacion.EsCorrecta, ["PuntosGanados"] = puntosGanados });

        // Notificar SIEMPRE que se registra una respuesta (correcta o no)
        await _notificador.NotificarRespuestaRegistradaAsync(
            comando.SesionId, comando.Dto.PreguntaId, contexto.EquipoId,
            verificacion.EsCorrecta, puntosGanados, cancelacion);

        // Notificar ranking solo si cambió (respuesta correcta)
        if (verificacion.EsCorrecta)
        {
            var ranking = await _consultas.ObtenerRankingAsync(comando.SesionId, cancelacion);
            try
            {
                var nombres = await _clienteSesiones.ObtenerNombresRankingAsync(comando.SesionId, cancelacion);
                if (nombres is not null)
                    EnriquecerNombres(ranking, nombres);
            }
            catch { /* non-fatal */ }
            await _notificador.NotificarPuntajeActualizadoAsync(comando.SesionId, ranking, cancelacion);
        }

        return new RespuestaTriviaResultadoDto
        {
            EsCorrecta = verificacion.EsCorrecta,
            PuntosGanados = puntosGanados,
            YaRespondida = false,
            Mensaje = verificacion.EsCorrecta
                ? $"¡Correcto! Ganaste {puntosGanados} puntos."
                : "Respuesta incorrecta."
        };
    }

    private static void EnriquecerNombres(
        IReadOnlyList<RankingEntradaDto> ranking,
        NombresRankingClienteDto nombres)
    {
        var equiposPorId = nombres.Equipos.ToDictionary(e => e.Id, e => e.Nombre);
        var participantesPorId = nombres.Participantes.ToDictionary(p => p.IdentidadId, p => p.Alias);
        foreach (var entrada in ranking)
        {
            if (entrada.EquipoId is Guid eqId && equiposPorId.TryGetValue(eqId, out var nombre))
                entrada.Nombre = nombre;
            else if (entrada.ParticipanteId is Guid partId && participantesPorId.TryGetValue(partId, out var alias))
                entrada.Nombre = alias;
        }
    }
}
