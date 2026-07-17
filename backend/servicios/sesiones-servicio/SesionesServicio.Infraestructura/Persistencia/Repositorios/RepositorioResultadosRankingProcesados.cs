using Microsoft.EntityFrameworkCore;
using SesionesServicio.Aplicacion.Puertos;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioResultadosRankingProcesados
    : IRepositorioResultadosRankingProcesados
{
    private readonly ContextoSesiones _contexto;

    public RepositorioResultadosRankingProcesados(ContextoSesiones contexto)
    {
        _contexto = contexto;
    }

    public Task<bool> ExisteAsync(
        Guid eventoIdOrigen,
        string tipoResultado,
        CancellationToken cancelacion)
        => _contexto.ResultadosRankingProcesados
            .AsNoTracking()
            .AnyAsync(r => r.EventoIdOrigen == eventoIdOrigen
                && r.TipoResultado == tipoResultado, cancelacion);

    public Task RegistrarAsync(
        Guid eventoIdOrigen,
        string tipoResultado,
        DateTime procesadoEnUtc,
        CancellationToken cancelacion)
    {
        _contexto.ResultadosRankingProcesados.Add(new ResultadoRankingProcesadoModelo
        {
            EventoIdOrigen = eventoIdOrigen,
            TipoResultado = tipoResultado,
            ProcesadoEnUtc = procesadoEnUtc
        });
        return Task.CompletedTask;
    }
}
