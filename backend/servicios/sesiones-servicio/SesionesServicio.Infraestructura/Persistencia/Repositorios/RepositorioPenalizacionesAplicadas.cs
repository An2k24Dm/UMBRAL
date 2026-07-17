using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Dominio.Eventos;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioPenalizacionesAplicadas : IRepositorioPenalizacionesAplicadas
{
    private readonly ContextoSesiones _contexto;

    public RepositorioPenalizacionesAplicadas(ContextoSesiones contexto)
    {
        _contexto = contexto;
    }

    public Task AgregarAsync(PenalizacionAplicada penalizacion, CancellationToken cancelacion)
    {
        _contexto.PenalizacionesAplicadas.Add(HaciaModelo(penalizacion));
        return Task.CompletedTask;
    }

    public Task<bool> ExistePorEventoIdAsync(Guid eventoId, CancellationToken cancelacion)
        => _contexto.PenalizacionesAplicadas
            .AsNoTracking()
            .AnyAsync(p => p.EventoId == eventoId, cancelacion);

    public async Task<IReadOnlyList<PenalizacionAplicada>> ListarPorSesionAsync(
        Guid sesionId, CancellationToken cancelacion)
        => await _contexto.PenalizacionesAplicadas
            .AsNoTracking()
            .Where(p => p.SesionId == sesionId)
            .OrderBy(p => p.AplicadaEnUtc)
            .Select(p => HaciaDominio(p))
            .ToListAsync(cancelacion);

    public async Task<IReadOnlyList<PenalizacionAplicada>> ListarPorParticipanteAsync(
        Guid sesionId, Guid participanteIdentidadId, CancellationToken cancelacion)
        => await _contexto.PenalizacionesAplicadas
            .AsNoTracking()
            .Where(p => p.SesionId == sesionId
                && p.ParticipanteIdentidadId == participanteIdentidadId)
            .OrderBy(p => p.AplicadaEnUtc)
            .Select(p => HaciaDominio(p))
            .ToListAsync(cancelacion);

    public async Task<IReadOnlyList<PenalizacionAplicada>> ListarPorEquipoAsync(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion)
        => await _contexto.PenalizacionesAplicadas
            .AsNoTracking()
            .Where(p => p.SesionId == sesionId && p.EquipoId == equipoId)
            .OrderBy(p => p.AplicadaEnUtc)
            .Select(p => HaciaDominio(p))
            .ToListAsync(cancelacion);

    public async Task<int> SumarPuntosPorParticipanteAsync(
        Guid sesionId, Guid participanteIdentidadId, CancellationToken cancelacion)
        => await _contexto.PenalizacionesAplicadas
            .AsNoTracking()
            .Where(p => p.SesionId == sesionId
                && p.ParticipanteIdentidadId == participanteIdentidadId)
            .SumAsync(p => p.Puntos, cancelacion);

    public async Task<int> SumarPuntosPorEquipoAsync(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion)
        => await _contexto.PenalizacionesAplicadas
            .AsNoTracking()
            .Where(p => p.SesionId == sesionId && p.EquipoId == equipoId)
            .SumAsync(p => p.Puntos, cancelacion);

    private static PenalizacionAplicadaModelo HaciaModelo(PenalizacionAplicada p) => new()
    {
        EventoId = p.EventoId,
        SesionId = p.SesionId,
        TipoObjetivo = (int)p.TipoObjetivo,
        ParticipanteSesionId = p.ParticipanteSesionId,
        ParticipanteIdentidadId = p.ParticipanteIdentidadId,
        EquipoId = p.EquipoId,
        Puntos = p.PuntosDescontados,
        Motivo = p.Motivo,
        OperadorIdentidadId = p.OperadorIdentidadId,
        AplicadaEnUtc = p.AplicadaEnUtc
    };

    private static PenalizacionAplicada HaciaDominio(PenalizacionAplicadaModelo p)
        => PenalizacionAplicada.Rehidratar(
            p.EventoId,
            p.SesionId,
            (TipoObjetivoPenalizacion)p.TipoObjetivo,
            p.ParticipanteSesionId,
            p.ParticipanteIdentidadId,
            p.EquipoId,
            p.Puntos,
            p.Motivo,
            p.OperadorIdentidadId,
            p.AplicadaEnUtc);
}
