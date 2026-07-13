using Microsoft.EntityFrameworkCore;
using Npgsql;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioEvidenciasTesoro : IRepositorioEvidenciasTesoro
{
    // Código SQLSTATE de PostgreSQL para violación de restricción única.
    private const string CodigoViolacionUnicidad = "23505";

    private readonly ContextoSesiones _contexto;

    public RepositorioEvidenciasTesoro(ContextoSesiones contexto) => _contexto = contexto;

    public async Task AgregarAsync(EvidenciaTesoroRegistro registro, CancellationToken cancelacion)
    {
        _contexto.EvidenciasTesoro.Add(new EvidenciaTesoroModelo
        {
            Id = Guid.NewGuid(),
            SesionId = registro.SesionId,
            MisionId = registro.MisionId,
            EtapaId = registro.EtapaId,
            BusquedaId = registro.BusquedaId,
            ParticipanteIdentidadId = registro.ParticipanteIdentidadId,
            EquipoId = registro.EquipoId,
            CodigoEnviado = registro.CodigoEnviado,
            EsValida = registro.EsValida,
            PuntosGanados = registro.PuntosGanados,
            EventoPuntuacionId = registro.EventoPuntuacionId,
            FechaEnvioUtc = registro.FechaEnvioUtc
        });
        try
        {
            await _contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException ex) when (EsViolacionUnicidad(ex))
        {
            // Otro integrante del equipo (o el mismo participante) ganó la carrera:
            // el índice único filtrado (es_valida = true) rechazó esta inserción.
            // Se desacopla la entidad fallida y se convierte en conflicto de negocio.
            foreach (var entrada in ex.Entries)
                entrada.State = EntityState.Detached;
            throw new EvidenciaTesoroDuplicadaExcepcion(esEquipo: registro.EquipoId.HasValue);
        }
    }

    public Task<bool> ExisteEvidenciaValidaIndividualAsync(
        Guid sesionId, Guid etapaId, Guid participanteIdentidadId, CancellationToken cancelacion)
        => _contexto.EvidenciasTesoro.AsNoTracking()
            .AnyAsync(e => e.SesionId == sesionId && e.EtapaId == etapaId
                && e.EquipoId == null
                && e.ParticipanteIdentidadId == participanteIdentidadId
                && e.EsValida, cancelacion);

    public Task<bool> ExisteEvidenciaValidaEquipoAsync(
        Guid sesionId, Guid etapaId, Guid equipoId, CancellationToken cancelacion)
        => _contexto.EvidenciasTesoro.AsNoTracking()
            .AnyAsync(e => e.SesionId == sesionId && e.EtapaId == etapaId
                && e.EquipoId == equipoId
                && e.EsValida, cancelacion);

    // Participantes distintos con evidencia válida en sesión individual (equipo_id IS NULL).
    public async Task<int> ContarParticipantesConEvidenciaValidaAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion)
    {
        var ids = await _contexto.EvidenciasTesoro.AsNoTracking()
            .Where(e => e.SesionId == sesionId && e.EtapaId == etapaId
                && e.EquipoId == null && e.EsValida)
            .Select(e => e.ParticipanteIdentidadId)
            .ToListAsync(cancelacion);
        return ids.Distinct().Count();
    }

    // Equipos distintos con evidencia válida en sesión grupal (equipo_id IS NOT NULL).
    public async Task<int> ContarEquiposConEvidenciaValidaAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion)
    {
        var ids = await _contexto.EvidenciasTesoro.AsNoTracking()
            .Where(e => e.SesionId == sesionId && e.EtapaId == etapaId
                && e.EquipoId != null && e.EsValida)
            .Select(e => e.EquipoId!.Value)
            .ToListAsync(cancelacion);
        return ids.Distinct().Count();
    }

    public async Task<IReadOnlyList<ProgresoTesoroItem>> ObtenerProgresoTesoroAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var registros = await _contexto.EvidenciasTesoro.AsNoTracking()
            .Where(e => e.SesionId == sesionId)
            .Select(e => new { e.ParticipanteIdentidadId, e.EquipoId, e.EsValida, e.PuntosGanados })
            .ToListAsync(cancelacion);

        return registros
            .GroupBy(e => e.EquipoId.HasValue
                ? $"e:{e.EquipoId.Value}"
                : $"p:{e.ParticipanteIdentidadId}")
            .Select(g => new ProgresoTesoroItem(
                g.Select(e => e.ParticipanteIdentidadId).First(),
                g.Select(e => e.EquipoId).First(),
                TotalIntentados: g.Count(),
                Validos: g.Count(e => e.EsValida),
                PuntosGanados: g.Sum(e => e.PuntosGanados)))
            .ToList()
            .AsReadOnly();
    }

    public Task<int> ActualizarPuntosGanadosPorEventoAsync(
        Guid eventoPuntuacionId, int puntosGanados, CancellationToken cancelacion)
        => _contexto.EvidenciasTesoro
            .Where(e => e.EventoPuntuacionId == eventoPuntuacionId)
            .ExecuteUpdateAsync(
                s => s.SetProperty(e => e.PuntosGanados, puntosGanados), cancelacion);

    public async Task<IReadOnlyList<PuntajeEtapaItem>> ObtenerPuntajePorEtapaParticipanteAsync(
        Guid sesionId, Guid participanteIdentidadId, CancellationToken cancelacion)
    {
        var filas = await _contexto.EvidenciasTesoro
            .AsNoTracking()
            .Where(e => e.SesionId == sesionId &&
                        e.ParticipanteIdentidadId == participanteIdentidadId)
            .GroupBy(e => new { e.MisionId, e.EtapaId })
            .Select(g => new PuntajeEtapaItem(
                g.Key.MisionId, g.Key.EtapaId, g.Sum(e => e.PuntosGanados)))
            .ToListAsync(cancelacion);

        return filas.AsReadOnly();
    }

    private static bool EsViolacionUnicidad(DbUpdateException ex)
        => ex.InnerException is PostgresException postgres &&
           postgres.SqlState == CodigoViolacionUnicidad;
}
