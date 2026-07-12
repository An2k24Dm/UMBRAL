using Microsoft.EntityFrameworkCore;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Infraestructura.Persistencia.Modelos;

namespace RankingServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioRankingEquipo : IRepositorioRankingEquipo
{
    private readonly ContextoRanking _contexto;

    public RepositorioRankingEquipo(ContextoRanking contexto)
    {
        _contexto = contexto;
    }

    public async Task<EntradaRankingEquipo?> ObtenerPorSesionYEquipoAsync(
        Guid sesionId, Guid equipoId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.EntradasEquipo
            .FirstOrDefaultAsync(
                e => e.SesionId == sesionId && e.EquipoId == equipoId,
                cancelacion);

        return modelo is null ? null : Rehidratar(modelo);
    }

    public async Task<List<EntradaRankingEquipo>> ObtenerPorSesionAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var modelos = await _contexto.EntradasEquipo
            .Where(e => e.SesionId == sesionId)
            .OrderBy(e => e.Posicion)
            .ToListAsync(cancelacion);

        return modelos.Select(Rehidratar).ToList();
    }

    public async Task AgregarAsync(EntradaRankingEquipo entrada, CancellationToken cancelacion)
    {
        var modelo = AModelo(entrada);
        await _contexto.EntradasEquipo.AddAsync(modelo, cancelacion);
    }

    public Task ActualizarAsync(EntradaRankingEquipo entrada, CancellationToken cancelacion)
    {
        var modelo = _contexto.EntradasEquipo.Local
            .FirstOrDefault(m => m.Id == entrada.Id);

        if (modelo is null)
        {
            modelo = AModelo(entrada);
            _contexto.EntradasEquipo.Update(modelo);
        }
        else
        {
            modelo.PuntajeTotal = entrada.PuntajeTotal;
            modelo.EtapasCompletadas = entrada.EtapasCompletadas;
            modelo.Posicion = entrada.Posicion;
            modelo.UltimaActualizacionUtc = entrada.UltimaActualizacionUtc;
        }

        return Task.CompletedTask;
    }

    private static EntradaRankingEquipo Rehidratar(EntradaRankingEquipoModelo m)
        => EntradaRankingEquipo.Rehidratar(
            m.Id, m.SesionId, m.EquipoId, m.NombreEquipo,
            m.PuntajeTotal, m.EtapasCompletadas, m.Posicion, m.UltimaActualizacionUtc);

    private static EntradaRankingEquipoModelo AModelo(EntradaRankingEquipo e)
        => new()
        {
            Id = e.Id,
            SesionId = e.SesionId,
            EquipoId = e.EquipoId,
            NombreEquipo = e.NombreEquipo,
            PuntajeTotal = e.PuntajeTotal,
            EtapasCompletadas = e.EtapasCompletadas,
            Posicion = e.Posicion,
            UltimaActualizacionUtc = e.UltimaActualizacionUtc
        };
}
