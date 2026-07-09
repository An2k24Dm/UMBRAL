using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioEtapasCompletadas : IRepositorioEtapasCompletadas
{
    private readonly ContextoSesiones _contexto;

    public RepositorioEtapasCompletadas(ContextoSesiones contexto) => _contexto = contexto;

    public async Task RegistrarAsync(
        Guid sesionId, Guid etapaId, DateTime fechaUtc, CancellationToken cancelacion)
    {
        var existe = await _contexto.EtapasCompletadas.AsNoTracking()
            .AnyAsync(e => e.SesionId == sesionId && e.EtapaId == etapaId, cancelacion);
        if (existe) return;

        _contexto.EtapasCompletadas.Add(new EtapaCompletadaModelo
        {
            SesionId = sesionId,
            EtapaId = etapaId,
            FechaCompletadaUtc = fechaUtc
        });

        try { await _contexto.SaveChangesAsync(cancelacion); }
        catch (DbUpdateException) { /* Condición de carrera: ya existe, ignorar */ }
    }

    public Task<int> ContarAsync(Guid sesionId, CancellationToken cancelacion)
        => _contexto.EtapasCompletadas.AsNoTracking()
            .CountAsync(e => e.SesionId == sesionId, cancelacion);
}
