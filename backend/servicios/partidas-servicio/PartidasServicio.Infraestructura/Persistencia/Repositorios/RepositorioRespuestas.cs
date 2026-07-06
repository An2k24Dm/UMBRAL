using Microsoft.EntityFrameworkCore;
using PartidasServicio.Aplicacion.Puertos;
using PartidasServicio.Commons.Dtos;
using PartidasServicio.Dominio.Abstract;
using PartidasServicio.Dominio.Entidades;
using PartidasServicio.Infraestructura.Persistencia.Modelos;

namespace PartidasServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioRespuestas : IRepositorioRespuestas, IConsultasPartidas
{
    private readonly ContextoPartidas _contexto;

    public RepositorioRespuestas(ContextoPartidas contexto)
    {
        _contexto = contexto;
    }

    public async Task AgregarAsync(RespuestaTrivia respuesta, CancellationToken cancelacion)
    {
        var modelo = new RespuestaTriviaModelo
        {
            Id = respuesta.Id,
            SesionId = respuesta.SesionId,
            MisionId = respuesta.MisionId,
            EtapaId = respuesta.EtapaId,
            PreguntaId = respuesta.PreguntaId,
            OpcionSeleccionadaId = respuesta.OpcionSeleccionadaId,
            ParticipanteId = respuesta.ParticipanteId,
            EquipoId = respuesta.EquipoId,
            EsCorrecta = respuesta.EsCorrecta,
            PuntosGanados = respuesta.PuntosGanados,
            TiempoTardadoMs = respuesta.TiempoTardadoMs,
            FechaRespuestaUtc = respuesta.FechaRespuestaUtc
        };
        await _contexto.RespuestasTrivia.AddAsync(modelo, cancelacion);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public Task<bool> YaRespondioEquipoAsync(
        Guid sesionId, Guid preguntaId, Guid equipoId, CancellationToken cancelacion)
        => _contexto.RespuestasTrivia.AnyAsync(
            r => r.SesionId == sesionId && r.PreguntaId == preguntaId && r.EquipoId == equipoId,
            cancelacion);

    public Task<bool> YaRespondioParticipanteAsync(
        Guid sesionId, Guid preguntaId, Guid participanteId, CancellationToken cancelacion)
        => _contexto.RespuestasTrivia.AnyAsync(
            r => r.SesionId == sesionId && r.PreguntaId == preguntaId
                 && r.EquipoId == null && r.ParticipanteId == participanteId,
            cancelacion);

    public async Task<IReadOnlyList<RankingEntradaDto>> ObtenerRankingAsync(
        Guid sesionId, CancellationToken cancelacion)
    {
        // Modo grupal: agrupar por equipo
        var grupal = await _contexto.RespuestasTrivia
            .Where(r => r.SesionId == sesionId && r.EquipoId != null)
            .GroupBy(r => r.EquipoId!.Value)
            .Select(g => new RankingEntradaDto
            {
                EquipoId = g.Key,
                ParticipanteId = null,
                Nombre = string.Empty,
                PuntajeTotal = g.Sum(r => r.PuntosGanados),
                TiempoTotalMs = g.Sum(r => r.TiempoTardadoMs),
                RespuestasCorrectas = g.Count(r => r.EsCorrecta)
            })
            .ToListAsync(cancelacion);

        // Modo individual: agrupar por participante (sin equipo)
        var individual = await _contexto.RespuestasTrivia
            .Where(r => r.SesionId == sesionId && r.EquipoId == null)
            .GroupBy(r => r.ParticipanteId)
            .Select(g => new RankingEntradaDto
            {
                EquipoId = null,
                ParticipanteId = g.Key,
                Nombre = string.Empty,
                PuntajeTotal = g.Sum(r => r.PuntosGanados),
                TiempoTotalMs = g.Sum(r => r.TiempoTardadoMs),
                RespuestasCorrectas = g.Count(r => r.EsCorrecta)
            })
            .ToListAsync(cancelacion);

        var combinado = grupal.Concat(individual)
            .OrderByDescending(r => r.PuntajeTotal)
            .ThenBy(r => r.TiempoTotalMs)
            .Select((r, idx) => { r.Posicion = idx + 1; return r; })
            .ToList();

        return combinado;
    }
}
