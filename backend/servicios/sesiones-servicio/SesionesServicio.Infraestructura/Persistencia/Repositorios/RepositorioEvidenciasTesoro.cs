using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioEvidenciasTesoro : IRepositorioEvidenciasTesoro
{
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
            CodigoEnviado = registro.CodigoEnviado,
            EsValida = registro.EsValida,
            PuntosGanados = registro.PuntosGanados,
            FechaEnvioUtc = registro.FechaEnvioUtc
        });
        try
        {
            await _contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateException)
        {
            // Violación de índice único (sesion+etapa+participante): condición de carrera.
            throw new InvalidOperationException("Ya enviaste una evidencia para esta etapa.");
        }
    }

    public Task<bool> ExisteEvidenciaValidaAsync(
        Guid sesionId, Guid etapaId, Guid participanteIdentidadId, CancellationToken cancelacion)
        => _contexto.EvidenciasTesoro.AsNoTracking()
            .AnyAsync(e => e.SesionId == sesionId && e.EtapaId == etapaId
                && e.ParticipanteIdentidadId == participanteIdentidadId && e.EsValida, cancelacion);

    public Task<bool> ExisteEvidenciaAsync(
        Guid sesionId, Guid etapaId, Guid participanteIdentidadId, CancellationToken cancelacion)
        => _contexto.EvidenciasTesoro.AsNoTracking()
            .AnyAsync(e => e.SesionId == sesionId && e.EtapaId == etapaId
                && e.ParticipanteIdentidadId == participanteIdentidadId, cancelacion);

    // Cuenta participantes distintos que enviaron evidencia válida (para detectar si todos completaron).
    public async Task<int> ContarParticipantesConEvidenciaValidaAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion)
    {
        var ids = await _contexto.EvidenciasTesoro.AsNoTracking()
            .Where(e => e.SesionId == sesionId && e.EtapaId == etapaId && e.EsValida)
            .Select(e => e.ParticipanteIdentidadId)
            .ToListAsync(cancelacion);
        return ids.Distinct().Count();
    }
}
