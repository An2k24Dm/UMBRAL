using Microsoft.EntityFrameworkCore;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Infraestructura.Persistencia.Modelos;

namespace RankingServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioRankingGlobal : IRepositorioRankingGlobal
{
    private readonly ContextoRanking _contexto;

    public RepositorioRankingGlobal(ContextoRanking contexto)
    {
        _contexto = contexto;
    }

    public async Task<RankingGlobalParticipante?> ObtenerPorParticipanteAsync(
        Guid participanteIdentidadId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.RankingGlobal
            .FirstOrDefaultAsync(
                r => r.ParticipanteIdentidadId == participanteIdentidadId,
                cancelacion);

        return modelo is null ? null : Rehidratar(modelo);
    }

    public async Task<List<RankingGlobalParticipante>> ObtenerTopAsync(
        int cantidad, CancellationToken cancelacion)
    {
        var modelos = await _contexto.RankingGlobal
            .OrderByDescending(r => r.PuntajeAcumulado)
            .Take(cantidad)
            .ToListAsync(cancelacion);

        return modelos.Select(Rehidratar).ToList();
    }

    public async Task AgregarAsync(
        RankingGlobalParticipante entrada, CancellationToken cancelacion)
    {
        var modelo = AModelo(entrada);
        await _contexto.RankingGlobal.AddAsync(modelo, cancelacion);
    }

    public Task ActualizarAsync(
        RankingGlobalParticipante entrada, CancellationToken cancelacion)
    {
        var modelo = _contexto.RankingGlobal.Local
            .FirstOrDefault(m => m.Id == entrada.Id);

        if (modelo is null)
        {
            modelo = AModelo(entrada);
            _contexto.RankingGlobal.Update(modelo);
        }
        else
        {
            modelo.NombreParticipante = entrada.NombreParticipante;
            modelo.PuntajeAcumulado = entrada.PuntajeAcumulado;
            modelo.SesionesJugadas = entrada.SesionesJugadas;
            modelo.EtapasCompletadasTotal = entrada.EtapasCompletadasTotal;
            modelo.UltimaActualizacionUtc = entrada.UltimaActualizacionUtc;
        }

        return Task.CompletedTask;
    }

    private static RankingGlobalParticipante Rehidratar(RankingGlobalParticipanteModelo m)
        => RankingGlobalParticipante.Rehidratar(
            m.Id, m.ParticipanteIdentidadId, m.NombreParticipante,
            m.PuntajeAcumulado, m.SesionesJugadas, m.EtapasCompletadasTotal,
            m.UltimaActualizacionUtc);

    private static RankingGlobalParticipanteModelo AModelo(RankingGlobalParticipante r)
        => new()
        {
            Id = r.Id,
            ParticipanteIdentidadId = r.ParticipanteIdentidadId,
            NombreParticipante = r.NombreParticipante,
            PuntajeAcumulado = r.PuntajeAcumulado,
            SesionesJugadas = r.SesionesJugadas,
            EtapasCompletadasTotal = r.EtapasCompletadasTotal,
            UltimaActualizacionUtc = r.UltimaActualizacionUtc
        };
}
