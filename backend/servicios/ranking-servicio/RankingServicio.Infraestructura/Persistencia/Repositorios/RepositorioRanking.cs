using Microsoft.EntityFrameworkCore;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;

namespace RankingServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioRanking : IRepositorioRanking
{
    private readonly ContextoRanking _contexto;

    public RepositorioRanking(ContextoRanking contexto)
    {
        _contexto = contexto;
    }

    public async Task<Ranking?> ObtenerPorSesionAsync(
        Guid sesionId, CancellationToken cancelacion)
        => await _contexto.Rankings
            .Include(r => r.Participantes)
            .Include(r => r.Equipos)
            .FirstOrDefaultAsync(r => r.SesionId == sesionId, cancelacion);

    public async Task AgregarAsync(Ranking ranking, CancellationToken cancelacion)
        => await _contexto.Rankings.AddAsync(ranking, cancelacion);

    // El agregado se obtiene rastreado (o se agrega como nuevo); EF detecta los
    // cambios de sus hijos y los persiste al guardar la unidad de trabajo.
    public Task ActualizarAsync(Ranking ranking, CancellationToken cancelacion)
        => Task.CompletedTask;
}
