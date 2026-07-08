using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioRespuestasTrivia : IRepositorioRespuestasTrivia
{
    private readonly ContextoSesiones _contexto;

    public RepositorioRespuestasTrivia(ContextoSesiones contexto)
    {
        _contexto = contexto;
    }

    public async Task AgregarAsync(RespuestaTriviaRegistro registro, CancellationToken cancelacion)
    {
        _contexto.RespuestasTrivia.Add(new RespuestaTriviaModelo
        {
            Id = Guid.NewGuid(),
            SesionId = registro.SesionId,
            MisionId = registro.MisionId,
            EtapaId = registro.EtapaId,
            TriviaId = registro.TriviaId,
            PreguntaId = registro.PreguntaId,
            OpcionSeleccionadaId = registro.OpcionSeleccionadaId,
            ParticipanteIdentidadId = registro.ParticipanteIdentidadId,
            EquipoId = registro.EquipoId,
            EsCorrecta = registro.EsCorrecta,
            PuntosGanados = registro.PuntosGanados,
            TiempoTardadoMs = registro.TiempoTardadoMs,
            FechaRespuestaUtc = registro.FechaRespuestaUtc
        });
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public Task<bool> ExisteRespuestaAsync(
        Guid sesionId, Guid etapaId, Guid preguntaId,
        Guid participanteIdentidadId, CancellationToken cancelacion)
        => _contexto.RespuestasTrivia
            .AsNoTracking()
            .AnyAsync(r =>
                r.SesionId == sesionId &&
                r.EtapaId == etapaId &&
                r.PreguntaId == preguntaId &&
                r.ParticipanteIdentidadId == participanteIdentidadId,
                cancelacion);

    public Task<int> ContarRespuestasDeJugadorEnEtapaAsync(
        Guid sesionId, Guid etapaId,
        Guid participanteIdentidadId, CancellationToken cancelacion)
        => _contexto.RespuestasTrivia
            .AsNoTracking()
            .CountAsync(r =>
                r.SesionId == sesionId &&
                r.EtapaId == etapaId &&
                r.ParticipanteIdentidadId == participanteIdentidadId,
                cancelacion);

    // Cuenta cuántos jugadores distintos han respondido todas las preguntas de la etapa.
    // Se trae la data a memoria para evitar que EF Core falle al traducir el GroupBy condicional.
    public async Task<int> ContarJugadoresQueCompletaronEtapaAsync(
        Guid sesionId, Guid etapaId, int totalPreguntas, CancellationToken cancelacion)
    {
        var registros = await _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r => r.SesionId == sesionId && r.EtapaId == etapaId)
            .Select(r => new { r.EquipoId, r.ParticipanteIdentidadId })
            .ToListAsync(cancelacion);

        return registros
            .GroupBy(r => r.EquipoId.HasValue
                ? $"e:{r.EquipoId}"
                : $"p:{r.ParticipanteIdentidadId}")
            .Count(g => g.Count() >= totalPreguntas);
    }

    public async Task<IReadOnlyList<Guid>> ObtenerPreguntasRespondidasAsync(
        Guid sesionId, Guid etapaId,
        Guid participanteIdentidadId, CancellationToken cancelacion)
    {
        var ids = await _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r =>
                r.SesionId == sesionId &&
                r.EtapaId == etapaId &&
                r.ParticipanteIdentidadId == participanteIdentidadId)
            .Select(r => r.PreguntaId)
            .ToListAsync(cancelacion);

        return ids.AsReadOnly();
    }
}
