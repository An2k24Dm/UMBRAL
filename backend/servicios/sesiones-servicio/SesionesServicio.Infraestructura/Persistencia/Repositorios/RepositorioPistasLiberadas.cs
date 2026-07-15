using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioPistasLiberadas : IRepositorioPistasLiberadas
{
    private readonly ContextoSesiones _contexto;

    public RepositorioPistasLiberadas(ContextoSesiones contexto) => _contexto = contexto;

    public async Task AgregarAsync(PistaLiberadaRegistro registro, CancellationToken cancelacion)
    {
        _contexto.PistasLiberadas.Add(new PistaLiberadaModelo
        {
            Id = Guid.NewGuid(),
            SesionId = registro.SesionId,
            EtapaId = registro.EtapaId,
            PistaId = registro.PistaId,
            Contenido = registro.Contenido,
            Tipo = registro.Tipo,
            Latitud = registro.Latitud,
            Longitud = registro.Longitud,
            FechaLiberacionUtc = registro.FechaLiberacionUtc
        });
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public Task<bool> ExistePistaLiberadaAsync(
        Guid sesionId, Guid etapaId, Guid pistaId, CancellationToken cancelacion)
        => _contexto.PistasLiberadas.AsNoTracking()
            .AnyAsync(p => p.SesionId == sesionId && p.EtapaId == etapaId
                && p.PistaId == pistaId, cancelacion);

    public async Task<List<PistaLiberadaRegistro>> ObtenerPorEtapaAsync(
        Guid sesionId, Guid etapaId, CancellationToken cancelacion)
    {
        var modelos = await _contexto.PistasLiberadas.AsNoTracking()
            .Where(p => p.SesionId == sesionId && p.EtapaId == etapaId)
            .OrderBy(p => p.FechaLiberacionUtc)
            .ToListAsync(cancelacion);

        return modelos.Select(m => new PistaLiberadaRegistro(
            m.SesionId, m.EtapaId, m.PistaId, m.Contenido,
            m.Tipo, m.Latitud, m.Longitud, m.FechaLiberacionUtc))
            .ToList();
    }
}
