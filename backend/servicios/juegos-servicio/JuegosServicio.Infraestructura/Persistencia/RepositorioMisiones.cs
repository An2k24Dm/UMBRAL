using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace JuegosServicio.Infraestructura.Persistencia;

public sealed class RepositorioMisiones : IRepositorioMisiones
{
    private readonly ContextoJuegos _contexto;

    public RepositorioMisiones(ContextoJuegos contexto)
    {
        _contexto = contexto;
    }

    public async Task<bool> EsContenidoUsadoEnEtapaAsync(
        TipoModoDeJuego tipo, Guid contenidoId, CancellationToken cancelacion)
    {
        return await _contexto.Etapas.AsNoTracking()
            .AnyAsync(e => e.TipoModoDeJuego == (int)tipo && e.ModoDeJuegoId == contenidoId, cancelacion);
    }

    public async Task<bool> EsContenidoUsadoEnMisionActivaAsync(
        TipoModoDeJuego tipo, Guid contenidoId, CancellationToken cancelacion)
    {
        var estadoActiva = (int)EstadoMision.Activa;
        return await _contexto.Etapas.AsNoTracking()
            .AnyAsync(e => e.TipoModoDeJuego == (int)tipo
                        && e.ModoDeJuegoId == contenidoId
                        && e.Mision.Estado == estadoActiva, cancelacion);
    }

    public async Task<bool> ExisteMisionConNombreAsync(string nombre, CancellationToken cancelacion)
    {
        var normalizado = nombre.Trim();
        return await _contexto.Misiones.AsNoTracking()
            .AnyAsync(m => m.Nombre == normalizado, cancelacion);
    }

    public async Task CrearMisionAsync(Mision mision, CancellationToken cancelacion)
    {
        var modelo = MisionesMapeador.AModelo(mision);
        _contexto.Misiones.Add(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task<Mision?> ObtenerMisionPorIdAsync(Guid misionId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Misiones
            .Include(m => m.Etapas)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == misionId, cancelacion);

        return modelo is null ? null : MisionesMapeador.ADominio(modelo);
    }

    public async Task<List<MisionResumenDto>> ObtenerMisionesEnBorradorAsync(
        Guid? creadorId, CancellationToken cancelacion)
    {
        var estadoActiva = (int)EstadoMision.Activa;

        return await _contexto.Misiones
            .AsNoTracking()
            .Where(m => (creadorId == null || m.CreadorId == creadorId) && m.Estado != estadoActiva)
            .OrderByDescending(m => m.FechaCreacion)
            .Select(m => new MisionResumenDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Descripcion = m.Descripcion,
                Estado = ((EstadoMision)m.Estado).ToString(),
                Dificultad = ((NivelDificultad)m.Dificultad).ToString(),
                TotalEtapas = m.Etapas.Count,
                FechaCreacion = m.FechaCreacion
            })
            .ToListAsync(cancelacion);
    }

    public async Task<List<MisionResumenDto>> ObtenerMisionesActivasAsync(CancellationToken cancelacion)
    {
        var estadoActiva = (int)EstadoMision.Activa;

        return await _contexto.Misiones
            .AsNoTracking()
            .Where(m => m.Estado == estadoActiva)
            .OrderBy(m => m.Nombre)
            .Select(m => new MisionResumenDto
            {
                Id = m.Id,
                Nombre = m.Nombre,
                Descripcion = m.Descripcion,
                Estado = nameof(EstadoMision.Activa),
                Dificultad = ((NivelDificultad)m.Dificultad).ToString(),
                TotalEtapas = m.Etapas.Count,
                FechaCreacion = m.FechaCreacion
            })
            .ToListAsync(cancelacion);
    }

    public async Task<MisionDetalleDto?> ObtenerDetalleMisionAsync(Guid misionId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Misiones
            .Include(m => m.Etapas.OrderBy(e => e.Orden))
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == misionId, cancelacion);

        if (modelo is null) return null;

        // Resuelve nombres de modos de juego en una sola query por tipo.
        var idsBusquedas = modelo.Etapas
            .Where(e => e.TipoModoDeJuego == (int)TipoModoDeJuego.BusquedaTesoro)
            .Select(e => e.ModoDeJuegoId).ToHashSet();
        var idsTrivias = modelo.Etapas
            .Where(e => e.TipoModoDeJuego == (int)TipoModoDeJuego.Trivia)
            .Select(e => e.ModoDeJuegoId).ToHashSet();

        var datosBusquedas = await _contexto.BusquedasTesoro.AsNoTracking()
            .Where(b => idsBusquedas.Contains(b.Id))
            .Select(b => new { b.Id, b.Nombre, b.Tiempo })
            .ToDictionaryAsync(b => b.Id, cancelacion);

        var datosTrivias = await _contexto.Trivias.AsNoTracking()
            .Where(t => idsTrivias.Contains(t.Id))
            .Select(t => new { t.Id, t.Nombre })
            .ToDictionaryAsync(t => t.Id, cancelacion);

        // Tiempo de cada trivia = suma de tiempoEstimado de sus preguntas
        var tiemposPorTrivia = await _contexto.Preguntas.AsNoTracking()
            .Where(p => idsTrivias.Contains(p.TriviaId))
            .GroupBy(p => p.TriviaId)
            .Select(g => new { TriviaId = g.Key, Tiempo = g.Sum(p => p.TiempoEstimado) })
            .ToDictionaryAsync(g => g.TriviaId, g => g.Tiempo, cancelacion);

        var etapas = modelo.Etapas.Select(e =>
        {
            var esBusqueda = e.TipoModoDeJuego == (int)TipoModoDeJuego.BusquedaTesoro;
            var nombre = esBusqueda
                ? datosBusquedas.GetValueOrDefault(e.ModoDeJuegoId)?.Nombre ?? "?"
                : datosTrivias.GetValueOrDefault(e.ModoDeJuegoId)?.Nombre ?? "?";
            var tiempo = esBusqueda
                ? datosBusquedas.GetValueOrDefault(e.ModoDeJuegoId)?.Tiempo ?? 0
                : tiemposPorTrivia.GetValueOrDefault(e.ModoDeJuegoId, 0);
            return new EtapaDetalleDto
            {
                Id = e.Id,
                Orden = e.Orden,
                TipoModoDeJuego = ((TipoModoDeJuego)e.TipoModoDeJuego).ToString(),
                ModoDeJuegoId = e.ModoDeJuegoId,
                NombreModoDeJuego = nombre,
                TiempoEstimado = tiempo
            };
        }).ToList();

        return new MisionDetalleDto
        {
            Id = modelo.Id,
            Nombre = modelo.Nombre,
            Descripcion = modelo.Descripcion,
            Estado = ((EstadoMision)modelo.Estado).ToString(),
            Dificultad = ((NivelDificultad)modelo.Dificultad).ToString(),
            FechaCreacion = modelo.FechaCreacion,
            TiempoTotal = etapas.Sum(e => e.TiempoEstimado),
            Etapas = etapas
        };
    }

    public async Task AgregarEtapaAsync(Etapa etapa, CancellationToken cancelacion)
    {
        var modelo = MisionesMapeador.AModelo(etapa);
        _contexto.Etapas.Add(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task EliminarEtapaAsync(Guid etapaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Etapas.FirstOrDefaultAsync(e => e.Id == etapaId, cancelacion);
        if (modelo is null) return;

        _contexto.Etapas.Remove(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ActualizarOrdenesEtapasAsync(
        IEnumerable<Etapa> etapas, CancellationToken cancelacion)
    {
        var ids = etapas.Select(e => e.Id).ToList();
        var modelos = await _contexto.Etapas.Where(e => ids.Contains(e.Id)).ToListAsync(cancelacion);
        var ordenPorId = etapas.ToDictionary(e => e.Id, e => e.Orden);
        foreach (var modelo in modelos)
            modelo.Orden = ordenPorId[modelo.Id];

        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ActualizarMisionAsync(Mision mision, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Misiones.FirstOrDefaultAsync(m => m.Id == mision.Id, cancelacion);
        if (modelo is null) return;

        modelo.Nombre = mision.Nombre;
        modelo.Descripcion = mision.Descripcion;
        modelo.Dificultad = (int)mision.Dificultad;
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ActivarMisionAsync(Mision mision, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Misiones.FirstOrDefaultAsync(m => m.Id == mision.Id, cancelacion);
        if (modelo is null) return;

        modelo.Estado = (int)EstadoMision.Activa;
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task DesactivarMisionAsync(Mision mision, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Misiones.FirstOrDefaultAsync(m => m.Id == mision.Id, cancelacion);
        if (modelo is null) return;

        modelo.Estado = (int)EstadoMision.Inactiva;
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task EliminarMisionAsync(Guid misionId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Misiones.FirstOrDefaultAsync(m => m.Id == misionId, cancelacion);
        if (modelo is null) return;

        _contexto.Misiones.Remove(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }
}
