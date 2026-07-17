using Microsoft.EntityFrameworkCore;
using Npgsql;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioRespuestasTrivia : IRepositorioRespuestasTrivia
{
    private const string CodigoViolacionUnicidad = "23505";

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
            EventoPuntuacionId = registro.EventoPuntuacionId,
            TiempoTardadoMs = registro.TiempoTardadoMs,
            FechaRespuestaUtc = registro.FechaRespuestaUtc
        });

        try
        {
            await _contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException ex) when (EsViolacionUnicidad(ex))
        {
            foreach (var entrada in ex.Entries)
                entrada.State = EntityState.Detached;
            throw new RespuestaTriviaDuplicadaExcepcion(esEquipo: registro.EquipoId.HasValue);
        }
    }

    public Task<bool> ExisteRespuestaOficialAsync(
        Guid sesionId, Guid etapaId, Guid preguntaId,
        Guid participanteIdentidadId, Guid? equipoId, CancellationToken cancelacion)
    {
        var consulta = _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r =>
                r.SesionId == sesionId &&
                r.EtapaId == etapaId &&
                r.PreguntaId == preguntaId);

        consulta = equipoId.HasValue
            ? consulta.Where(r => r.EquipoId == equipoId)
            : consulta.Where(r => r.EquipoId == null &&
                                  r.ParticipanteIdentidadId == participanteIdentidadId);

        return consulta.AnyAsync(cancelacion);
    }

    public Task<int> ContarPreguntasDistintasDeJugadorEnEtapaAsync(
        Guid sesionId, Guid etapaId,
        Guid participanteIdentidadId, Guid? equipoId, CancellationToken cancelacion)
    {
        var consulta = _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r => r.SesionId == sesionId && r.EtapaId == etapaId);

        consulta = equipoId.HasValue
            ? consulta.Where(r => r.EquipoId == equipoId)
            : consulta.Where(r => r.EquipoId == null &&
                                  r.ParticipanteIdentidadId == participanteIdentidadId);

        return consulta.Select(r => r.PreguntaId).Distinct().CountAsync(cancelacion);
    }

    public async Task<int> ContarJugadoresQueCompletaronEtapaAsync(
        Guid sesionId, Guid etapaId, int totalPreguntas, CancellationToken cancelacion)
    {
        if (totalPreguntas <= 0) return 0;

        var registros = await _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r => r.SesionId == sesionId && r.EtapaId == etapaId)
            .Select(r => new { r.EquipoId, r.ParticipanteIdentidadId, r.PreguntaId })
            .ToListAsync(cancelacion);

        return registros
            .GroupBy(r => r.EquipoId.HasValue
                ? $"e:{r.EquipoId}"
                : $"p:{r.ParticipanteIdentidadId}")
            .Count(g => g.Select(x => x.PreguntaId).Distinct().Count() >= totalPreguntas);
    }

    public async Task<IReadOnlyList<Guid>> ObtenerPreguntasRespondidasAsync(
        Guid sesionId, Guid etapaId,
        Guid participanteIdentidadId, Guid? equipoId, CancellationToken cancelacion)
    {
        var consulta = _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r => r.SesionId == sesionId && r.EtapaId == etapaId);

        consulta = equipoId.HasValue
            ? consulta.Where(r => r.EquipoId == equipoId)
            : consulta.Where(r => r.EquipoId == null &&
                                  r.ParticipanteIdentidadId == participanteIdentidadId);

        var ids = await consulta
            .Select(r => r.PreguntaId)
            .Distinct()
            .ToListAsync(cancelacion);

        return ids.AsReadOnly();
    }

    public async Task<IReadOnlyList<RespuestaTriviaTiempo>> ObtenerRespuestasConTiempoAsync(
        Guid sesionId, Guid etapaId,
        Guid participanteIdentidadId, Guid? equipoId, CancellationToken cancelacion)
    {
        var consulta = _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r => r.SesionId == sesionId && r.EtapaId == etapaId);

        consulta = equipoId.HasValue
            ? consulta.Where(r => r.EquipoId == equipoId)
            : consulta.Where(r => r.EquipoId == null &&
                                  r.ParticipanteIdentidadId == participanteIdentidadId);

        var filas = await consulta
            .Select(r => new { r.PreguntaId, r.TiempoTardadoMs })
            .ToListAsync(cancelacion);

        return filas
            .GroupBy(f => f.PreguntaId)
            .Select(g => new RespuestaTriviaTiempo(g.Key, g.Min(x => x.TiempoTardadoMs)))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<ProgresoTriviaItem>> ObtenerProgresoTriviaAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var registros = await _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r => r.SesionId == sesionId)
            .Select(r => new { r.ParticipanteIdentidadId, r.EquipoId, r.EsCorrecta, r.PuntosGanados })
            .ToListAsync(cancelacion);

        return registros
            .GroupBy(r => r.EquipoId.HasValue
                ? $"e:{r.EquipoId.Value}"
                : $"p:{r.ParticipanteIdentidadId}")
            .Select(g => new ProgresoTriviaItem(
                ParticipanteIdentidadId: g.Select(r => r.ParticipanteIdentidadId).First(),
                EquipoId: g.Select(r => r.EquipoId).First(),
                TotalRespondidas: g.Count(),
                Correctas: g.Count(r => r.EsCorrecta),
                PuntosGanados: g.Sum(r => r.PuntosGanados)))
            .ToList()
            .AsReadOnly();
    }

    public async Task<IReadOnlyList<ProgresoTriviaEtapaItem>> ObtenerProgresoTriviaPorEtapaAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var registros = await _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r => r.SesionId == sesionId)
            .Select(r => new
            {
                r.ParticipanteIdentidadId,
                r.EquipoId,
                r.EtapaId,
                r.PreguntaId
            })
            .ToListAsync(cancelacion);

        return registros
            .GroupBy(r => new
            {
                Clave = r.EquipoId.HasValue
                    ? $"e:{r.EquipoId.Value}"
                    : $"p:{r.ParticipanteIdentidadId}",
                r.EtapaId
            })
            .Select(g => new ProgresoTriviaEtapaItem(
                ParticipanteIdentidadId: g.Select(r => r.ParticipanteIdentidadId).First(),
                EquipoId: g.Select(r => r.EquipoId).First(),
                EtapaId: g.Key.EtapaId,
                PreguntasRespondidas: g.Select(r => r.PreguntaId).Distinct().Count()))
            .ToList()
            .AsReadOnly();
    }

    public Task<int> ActualizarPuntosGanadosPorEventoAsync(
        Guid eventoPuntuacionId, int puntosGanados, CancellationToken cancelacion)
        => _contexto.RespuestasTrivia
            .Where(r => r.EventoPuntuacionId == eventoPuntuacionId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(r => r.PuntosGanados, puntosGanados), cancelacion);

    public async Task<IReadOnlyList<PuntajeEtapaItem>> ObtenerPuntajePorEtapaParticipanteAsync(
        Guid sesionId, Guid participanteIdentidadId, CancellationToken cancelacion)
    {
        var filas = await _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r => r.SesionId == sesionId &&
                        r.ParticipanteIdentidadId == participanteIdentidadId)
            .GroupBy(r => new { r.MisionId, r.EtapaId })
            .Select(g => new PuntajeEtapaItem(
                g.Key.MisionId, g.Key.EtapaId, g.Sum(r => r.PuntosGanados)))
            .ToListAsync(cancelacion);

        return filas.AsReadOnly();
    }

    public async Task<long> ObtenerPuntajeGanadoEquipoAsync(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion)
        => await _contexto.RespuestasTrivia
            .AsNoTracking()
            .Where(r => r.SesionId == sesionId && r.EquipoId == equipoId)
            .SumAsync(r => (long)r.PuntosGanados, cancelacion);

    private static bool EsViolacionUnicidad(DbUpdateException ex)
        => ex.InnerException is PostgresException postgres &&
           postgres.SqlState == CodigoViolacionUnicidad;
}
