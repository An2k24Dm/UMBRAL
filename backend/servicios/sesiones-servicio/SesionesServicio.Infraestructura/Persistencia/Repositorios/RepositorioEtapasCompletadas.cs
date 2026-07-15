using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Abstract;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioEtapasCompletadas : IRepositorioEtapasCompletadas
{
    private readonly ContextoSesiones _contexto;

    public RepositorioEtapasCompletadas(ContextoSesiones contexto) => _contexto = contexto;

    public async Task<bool> RegistrarAsync(
        Guid sesionId, Guid etapaId, DateTime fechaUtc, CancellationToken cancelacion)
    {
        var filas = await _contexto.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO sesiones."EtapaCompletada"
                (sesion_id, etapa_id, fecha_completada_utc)
            VALUES ({sesionId}, {etapaId}, {fechaUtc})
            ON CONFLICT (sesion_id, etapa_id) DO NOTHING
            """, cancelacion);

        return filas > 0;
    }

    public Task<int> ContarAsync(Guid sesionId, CancellationToken cancelacion)
        => _contexto.EtapasCompletadas.AsNoTracking()
            .CountAsync(e => e.SesionId == sesionId, cancelacion);

    public async Task<IReadOnlyList<Guid>> ObtenerCompletadasAsync(
        Guid sesionId, CancellationToken cancelacion)
        => await _contexto.EtapasCompletadas.AsNoTracking()
            .Where(e => e.SesionId == sesionId)
            .OrderBy(e => e.FechaCompletadaUtc)
            .Select(e => e.EtapaId)
            .ToListAsync(cancelacion);
}
