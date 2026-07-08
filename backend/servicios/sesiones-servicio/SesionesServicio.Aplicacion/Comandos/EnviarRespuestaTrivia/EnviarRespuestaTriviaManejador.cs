using MediatR;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Aplicacion.Comandos.EnviarRespuestaTrivia;

public sealed class EnviarRespuestaTriviaManejador
    : IRequestHandler<EnviarRespuestaTriviaComando, EnviarRespuestaTriviaRespuesta>
{
    private readonly IUsuarioActual _usuario;
    private readonly IRepositorioSesiones _repositorioSesiones;
    private readonly IClienteJuegosTrivia _clienteTrivia;
    private readonly IRepositorioRespuestasTrivia _repositorioRespuestas;
    private readonly INotificadorSesionesTiempoReal _notificador;

    public EnviarRespuestaTriviaManejador(
        IUsuarioActual usuario,
        IRepositorioSesiones repositorioSesiones,
        IClienteJuegosTrivia clienteTrivia,
        IRepositorioRespuestasTrivia repositorioRespuestas,
        INotificadorSesionesTiempoReal notificador)
    {
        _usuario = usuario;
        _repositorioSesiones = repositorioSesiones;
        _clienteTrivia = clienteTrivia;
        _repositorioRespuestas = repositorioRespuestas;
        _notificador = notificador;
    }

    public async Task<EnviarRespuestaTriviaRespuesta> Handle(
        EnviarRespuestaTriviaComando comando, CancellationToken cancelacion)
    {
        var participanteIdentidadId = _usuario.ObtenerId()
            ?? throw new UnauthorizedAccessException("Usuario no autenticado.");

        var sesion = await _repositorioSesiones.ObtenerPorIdAsync(comando.SesionId, cancelacion)
            ?? throw new InvalidOperationException("Sesión no encontrada.");

        if (sesion.Estado != EstadoSesion.Activa)
            throw new InvalidOperationException("La sesión no está activa.");

        var (participante, totalJugadoresEsperados) = ObtenerParticipanteYTotal(sesion, participanteIdentidadId);

        // Idempotencia: no permitir responder dos veces la misma pregunta.
        var yaRespondio = await _repositorioRespuestas.ExisteRespuestaAsync(
            comando.SesionId, comando.EtapaId, comando.PreguntaId,
            participanteIdentidadId, cancelacion);

        if (yaRespondio)
            throw new InvalidOperationException("Ya respondiste esta pregunta.");

        // Verificar la respuesta en juegos-servicio y obtener puntaje + tiempo límite.
        var verificacion = await _clienteTrivia.VerificarRespuestaAsync(
            comando.TriviaId, comando.PreguntaId, comando.OpcionSeleccionadaId, cancelacion)
            ?? throw new InvalidOperationException("Pregunta u opción no encontrada.");

        var puntosGanados = CalcularPuntaje(
            verificacion.EsCorrecta,
            verificacion.PuntajeBase,
            comando.TiempoTardadoMs,
            verificacion.TiempoLimiteSegundos * 1000);

        // Persistir la respuesta.
        await _repositorioRespuestas.AgregarAsync(new RespuestaTriviaRegistro(
            SesionId: comando.SesionId,
            MisionId: comando.MisionId,
            EtapaId: comando.EtapaId,
            TriviaId: comando.TriviaId,
            PreguntaId: comando.PreguntaId,
            OpcionSeleccionadaId: comando.OpcionSeleccionadaId,
            ParticipanteIdentidadId: participanteIdentidadId,
            EquipoId: participante.EquipoId,
            EsCorrecta: verificacion.EsCorrecta,
            PuntosGanados: puntosGanados,
            TiempoTardadoMs: comando.TiempoTardadoMs,
            FechaRespuestaUtc: DateTime.UtcNow),
            cancelacion);

        // Notificar respuesta en tiempo real.
        await _notificador.NotificarRespuestaRegistradaAsync(
            comando.SesionId, comando.EtapaId, comando.PreguntaId,
            participanteIdentidadId, participante.EquipoId,
            verificacion.EsCorrecta, puntosGanados, cancelacion);

        // Comprobar si la etapa quedó completa para todos.
        var etapaCompletada = false;
        var respuestasDelJugador = await _repositorioRespuestas.ContarRespuestasDeJugadorEnEtapaAsync(
            comando.SesionId, comando.EtapaId, participanteIdentidadId, cancelacion);

        if (respuestasDelJugador >= comando.TotalPreguntasEtapa)
        {
            var jugadoresCompletaron = await _repositorioRespuestas.ContarJugadoresQueCompletaronEtapaAsync(
                comando.SesionId, comando.EtapaId, comando.TotalPreguntasEtapa, cancelacion);

            if (jugadoresCompletaron >= totalJugadoresEsperados)
            {
                etapaCompletada = true;
                await _notificador.NotificarEtapaCompletadaAsync(
                    comando.SesionId, comando.MisionId, comando.EtapaId, cancelacion);
            }
        }

        return new EnviarRespuestaTriviaRespuesta(
            verificacion.EsCorrecta, puntosGanados, etapaCompletada);
    }

    // Penalización por tiempo: 5 tramos de 20%.
    // Ejemplo: 5pts, 10s → 0-2s=5pts, 2-4s=4pts, 4-6s=3pts, 6-8s=2pts, 8-10s=1pt, >10s=0pts
    private static int CalcularPuntaje(bool esCorrecta, int puntajeBase, int tiempoTardadoMs, int tiempoLimiteMs)
    {
        if (!esCorrecta) return 0;
        if (tiempoLimiteMs <= 0 || tiempoTardadoMs >= tiempoLimiteMs) return 0;

        var tamanoTramo = tiempoLimiteMs / 5;
        var tramo = tamanoTramo > 0 ? tiempoTardadoMs / tamanoTramo : 0;
        return Math.Max(0, (int)(puntajeBase * (1.0 - tramo * 0.2)));
    }

    // Devuelve (participante encontrado, total jugadores en la sesión).
    // Individual: jugadores = participantes directos. Grupal: jugadores = equipos.
    private static (Participante participante, int totalJugadores) ObtenerParticipanteYTotal(
        Sesion sesion, Guid participanteIdentidadId)
    {
        if (sesion is SesionIndividual individual)
        {
            var p = individual.Participantes
                .FirstOrDefault(x => x.ParticipanteIdentidadId == participanteIdentidadId)
                ?? throw new InvalidOperationException(
                    "El participante no está inscrito en esta sesión.");
            return (p, individual.Participantes.Count);
        }

        if (sesion is SesionGrupal grupal)
        {
            foreach (var equipo in grupal.Equipos)
            {
                var p = equipo.Participantes
                    .FirstOrDefault(x => x.ParticipanteIdentidadId == participanteIdentidadId);
                if (p is not null)
                    return (p, grupal.Equipos.Count);
            }
            throw new InvalidOperationException("El participante no está inscrito en esta sesión.");
        }

        throw new InvalidOperationException("Tipo de sesión no soportado.");
    }
}
