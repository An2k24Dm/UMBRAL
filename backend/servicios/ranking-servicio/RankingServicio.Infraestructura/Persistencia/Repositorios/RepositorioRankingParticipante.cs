using Microsoft.EntityFrameworkCore;
using RankingServicio.Aplicacion.Puertos;
using RankingServicio.Dominio.Entidades;
using RankingServicio.Infraestructura.Persistencia.Modelos;

namespace RankingServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioRankingParticipante : IRepositorioRankingParticipante
{
    private readonly ContextoRanking _contexto;

    public RepositorioRankingParticipante(ContextoRanking contexto)
    {
        _contexto = contexto;
    }

    public async Task<EntradaRankingParticipante?> ObtenerPorSesionYParticipanteAsync(
        Guid sesionId, Guid participanteIdentidadId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.EntradasParticipante
            .FirstOrDefaultAsync(
                e => e.SesionId == sesionId &&
                     e.ParticipanteIdentidadId == participanteIdentidadId,
                cancelacion);

        return modelo is null ? null : Rehidratar(modelo);
    }

    public async Task<List<EntradaRankingParticipante>> ObtenerPorSesionAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        var modelos = await _contexto.EntradasParticipante
            .Where(e => e.SesionId == sesionId)
            .OrderBy(e => e.Posicion)
            .ToListAsync(cancelacion);

        return modelos.Select(Rehidratar).ToList();
    }

    public async Task AgregarAsync(
        EntradaRankingParticipante entrada, CancellationToken cancelacion)
    {
        var modelo = AModelo(entrada);
        await _contexto.EntradasParticipante.AddAsync(modelo, cancelacion);
    }

    public Task ActualizarAsync(
        EntradaRankingParticipante entrada, CancellationToken cancelacion)
    {
        var modelo = _contexto.EntradasParticipante.Local
            .FirstOrDefault(m => m.Id == entrada.Id);

        if (modelo is null)
        {
            modelo = AModelo(entrada);
            _contexto.EntradasParticipante.Update(modelo);
        }
        else
        {
            modelo.PuntajeTotal = entrada.PuntajeTotal;
            modelo.RespuestasCorrectas = entrada.RespuestasCorrectas;
            modelo.RespuestasTotales = entrada.RespuestasTotales;
            modelo.EtapasCompletadas = entrada.EtapasCompletadas;
            modelo.Posicion = entrada.Posicion;
            modelo.UltimaActualizacionUtc = entrada.UltimaActualizacionUtc;
        }

        return Task.CompletedTask;
    }

    private static EntradaRankingParticipante Rehidratar(EntradaRankingParticipanteModelo m)
        => EntradaRankingParticipante.Rehidratar(
            m.Id, m.SesionId, m.ParticipanteIdentidadId, m.NombreParticipante,
            m.PuntajeTotal, m.RespuestasCorrectas, m.RespuestasTotales,
            m.EtapasCompletadas, m.Posicion, m.UltimaActualizacionUtc);

    private static EntradaRankingParticipanteModelo AModelo(EntradaRankingParticipante e)
        => new()
        {
            Id = e.Id,
            SesionId = e.SesionId,
            ParticipanteIdentidadId = e.ParticipanteIdentidadId,
            NombreParticipante = e.NombreParticipante,
            PuntajeTotal = e.PuntajeTotal,
            RespuestasCorrectas = e.RespuestasCorrectas,
            RespuestasTotales = e.RespuestasTotales,
            EtapasCompletadas = e.EtapasCompletadas,
            Posicion = e.Posicion,
            UltimaActualizacionUtc = e.UltimaActualizacionUtc
        };
}
