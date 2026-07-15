using Microsoft.EntityFrameworkCore;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Infraestructura.Persistencia.Modelos;

namespace RankingServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioEventosProcesados : IRepositorioEventosProcesados
{
    private readonly ContextoRanking _contexto;

    public RepositorioEventosProcesados(ContextoRanking contexto)
    {
        _contexto = contexto;
    }

    public Task<bool> ExisteAsync(Guid eventoId, string tipoEvento, CancellationToken cancelacion)
        => _contexto.EventosProcesados
            .AnyAsync(e => e.Id == eventoId && e.TipoEvento == tipoEvento, cancelacion);

    public async Task RegistrarAsync(
        Guid eventoId, string tipoEvento, DateTime ahora, CancellationToken cancelacion)
    {
        await _contexto.EventosProcesados.AddAsync(new EventoProcesadoModelo
        {
            Id = eventoId,
            TipoEvento = tipoEvento,
            ProcesadoEnUtc = ahora
        }, cancelacion);
    }
}
