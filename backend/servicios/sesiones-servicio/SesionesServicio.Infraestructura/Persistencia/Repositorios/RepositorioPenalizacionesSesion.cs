using Microsoft.EntityFrameworkCore;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

// HU52 — Repositorio de penalizaciones. No confirma cambios: el guardado ocurre
// dentro de la unidad de trabajo/transacción del manejador (junto con el Outbox
// al registrar, o con el snapshot al procesar el resultado).
public sealed class RepositorioPenalizacionesSesion : IRepositorioPenalizacionesSesion
{
    private readonly ContextoSesiones _contexto;

    public RepositorioPenalizacionesSesion(ContextoSesiones contexto)
    {
        _contexto = contexto;
    }

    public Task AgregarAsync(PenalizacionSesion penalizacion, CancellationToken cancelacion)
    {
        _contexto.Penalizaciones.Add(HaciaModelo(penalizacion));
        return Task.CompletedTask;
    }

    public async Task<PenalizacionSesion?> ObtenerPorEventoIdAsync(
        Guid eventoId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Penalizaciones
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.EventoId == eventoId, cancelacion);
        return modelo is null ? null : HaciaDominio(modelo);
    }

    public async Task ActualizarAsync(PenalizacionSesion penalizacion, CancellationToken cancelacion)
    {
        var existente = await _contexto.Penalizaciones
            .FirstOrDefaultAsync(p => p.Id == penalizacion.Id, cancelacion);

        var actualizado = HaciaModelo(penalizacion);
        if (existente is null)
        {
            _contexto.Penalizaciones.Add(actualizado);
            return;
        }

        _contexto.Entry(existente).CurrentValues.SetValues(actualizado);
    }

    private static PenalizacionSesionModelo HaciaModelo(PenalizacionSesion p) => new()
    {
        Id = p.Id,
        EventoId = p.EventoId,
        SesionId = p.SesionId,
        TipoObjetivo = (int)p.TipoObjetivo,
        ParticipanteSesionId = p.ParticipanteSesionId,
        ParticipanteIdentidadId = p.ParticipanteIdentidadId,
        EquipoId = p.EquipoId,
        Puntos = p.Puntos,
        Motivo = p.Motivo,
        OperadorIdentidadId = p.OperadorIdentidadId,
        AplicadaEnUtc = p.AplicadaEnUtc,
        ProcesadaEnUtc = p.ProcesadaEnUtc,
        PuntajeResultante = p.PuntajeResultante,
        EstadoProcesamiento = (int)p.EstadoProcesamiento
    };

    private static PenalizacionSesion HaciaDominio(PenalizacionSesionModelo p)
        => PenalizacionSesion.Rehidratar(
            p.Id,
            p.EventoId,
            p.SesionId,
            (TipoObjetivoPenalizacion)p.TipoObjetivo,
            p.ParticipanteSesionId,
            p.ParticipanteIdentidadId,
            p.EquipoId,
            p.Puntos,
            p.Motivo,
            p.OperadorIdentidadId,
            p.AplicadaEnUtc,
            p.ProcesadaEnUtc,
            p.PuntajeResultante,
            (EstadoProcesamientoPenalizacion)p.EstadoProcesamiento);
}
