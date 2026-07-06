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
    private readonly INotificadorPartidasTiempoReal _notificador;
    private readonly ICalculadoraPuntaje _calculadora;
    private readonly IRegistroLogsAplicacion _logs;

    // Cadena de responsabilidad: Estado → Participante → Concurrencia
    private readonly IReadOnlyList<IEslabonValidacion> _cadena;

    public EnviarRespuestaTriviaManejador(
        IValidador<EnviarRespuestaTriviaComando> validador,
        IRepositorioRespuestas repositorio,
        IUnidadTrabajoPartidas unidadTrabajo,
        IUsuarioActual usuarioActual,
        IProveedorFechaHora reloj,
        IClienteJuegos clienteJuegos,
        IConsultasPartidas consultas,
        INotificadorPartidasTiempoReal notificador,
        ICalculadoraPuntaje calculadora,
        IRegistroLogsAplicacion logs,
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
        _calculadora = calculadora;
        _logs = logs;
        _cadena = [eslabonEstado, eslabonParticipante, eslabonConcurrencia];
    }

    public async Task<RespuestaTriviaResultadoDto> Handle(
        EnviarRespuestaTriviaComando comando, CancellationToken cancelacion)
    {
        _validador.Validar(comando).LanzarSiHayErrores();

        var participanteId = _usuarioActual.ObtenerId()
            ?? throw new ExcepcionDominio("No se pudo obtener el participante autenticado.");

        // Ejecutar la cadena de responsabilidad
        var contexto = new ContextoValidacionRespuesta
        {
            SesionId = comando.SesionId,
            PreguntaId = comando.Dto.PreguntaId,
            ParticipanteId = participanteId
        };

        foreach (var eslabon in _cadena)
            await eslabon.ValidarAsync(contexto, cancelacion);

        // Si ya fue respondida por el equipo/participante, retornar DTO informativo
        if (contexto.PreguntaYaRespondida)
        {
            _logs.Informacion(
                evento: "PreguntaYaRespondida",
                descripcion: "El equipo ya respondió esta pregunta previamente.",
                propiedades: new Dictionary<string, object?>
                {
                    ["SesionId"] = comando.SesionId,
                    ["PreguntaId"] = comando.Dto.PreguntaId,
                    ["EquipoId"] = contexto.EquipoId
                });

            return new RespuestaTriviaResultadoDto
            {
                YaRespondida = true,
                EsCorrecta = false,
                PuntosGanados = 0,
                Mensaje = "Tu equipo ya respondió esta pregunta."
            };
        }

        // Verificar correctitud contra juegos-servicio (Strategy necesita el puntaje base)
        var verificacion = await _clienteJuegos.VerificarRespuestaAsync(
            comando.TriviaId, comando.Dto.PreguntaId, comando.Dto.OpcionSeleccionadaId, cancelacion)
            ?? throw new PreguntaNoEncontradaExcepcion(comando.Dto.PreguntaId);

        // Calcular puntaje si es correcta (Strategy)
        var puntosGanados = verificacion.EsCorrecta
            ? _calculadora.Calcular(verificacion.PuntajeBase, comando.Dto.TiempoTardadoMs, verificacion.TiempoLimiteMs)
            : 0;

        // Crear y persistir la respuesta
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
            // Carrera entre dos miembros del mismo equipo: el repositorio tradujo
            // la violación de unique constraint a excepción de dominio.
            _logs.Advertencia(
                evento: "ConcurrenciaRespuestaTrivia",
                descripcion: "Colisión de concurrencia al guardar respuesta; ya existe para este equipo/participante.",
                propiedades: new Dictionary<string, object?>
                {
                    ["SesionId"] = comando.SesionId,
                    ["PreguntaId"] = comando.Dto.PreguntaId,
                    ["EquipoId"] = contexto.EquipoId
                });

            return new RespuestaTriviaResultadoDto
            {
                YaRespondida = true,
                EsCorrecta = false,
                PuntosGanados = 0,
                Mensaje = "Tu equipo ya respondió esta pregunta."
            };
        }

        _logs.Informacion(
            evento: "RespuestaTriviaRegistrada",
            descripcion: "Respuesta de trivia registrada correctamente.",
            propiedades: new Dictionary<string, object?>
            {
                ["SesionId"] = comando.SesionId,
                ["PreguntaId"] = comando.Dto.PreguntaId,
                ["EsCorrecta"] = verificacion.EsCorrecta,
                ["PuntosGanados"] = puntosGanados
            });

        // Notificar ranking actualizado en tiempo real (SignalR)
        if (verificacion.EsCorrecta)
        {
            var ranking = await _consultas.ObtenerRankingAsync(comando.SesionId, cancelacion);
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

}
