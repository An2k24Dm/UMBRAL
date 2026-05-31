using JuegosServicio.Aplicacion.Puertos;
using JuegosServicio.Commons.Dtos;
using JuegosServicio.Dominio.Entidades;
using JuegosServicio.Dominio.Enums;
using Microsoft.EntityFrameworkCore;
using JuegosServicio.Infraestructura.Persistencia.Modelos;

namespace JuegosServicio.Infraestructura.Persistencia;

public sealed class RepositorioBusquedas : IRepositorioBusquedas
{
    private readonly ContextoJuegos _contexto;

    public RepositorioBusquedas(ContextoJuegos contexto)
    {
        _contexto = contexto;
    }

    public async Task<bool> ExisteBusquedaConNombreAsync(string nombre, CancellationToken cancelacion)
    {
        var normalizado = nombre.Trim();
        return await _contexto.BusquedasTesoro.AsNoTracking()
            .AnyAsync(b => b.Nombre == normalizado, cancelacion);
    }

    public async Task CrearBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion)
    {
        var modelo = BusquedasMapeador.AModelo(busqueda);
        _contexto.BusquedasTesoro.Add(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task<BusquedaTesoro?> ObtenerBusquedaPorIdAsync(Guid busquedaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.BusquedasTesoro
            .Include(b => b.Etapas)
                .ThenInclude(e => e.Misiones)
            .Include(b => b.Etapas)
                .ThenInclude(e => e.Pistas)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == busquedaId, cancelacion);

        return modelo is null ? null : BusquedasMapeador.ADominio(modelo);
    }

    public async Task<List<BusquedaTesoroResumenDto>> ObtenerBusquedasEnBorradorAsync(
        Guid? creadorId, CancellationToken cancelacion)
    {
        var estadoActiva = (int)EstadoBusqueda.Activa;

        return await _contexto.BusquedasTesoro
            .AsNoTracking()
            .Where(b => (creadorId == null || b.CreadorId == creadorId) && b.Estado != estadoActiva)
            .OrderByDescending(b => b.FechaCreacion)
            .Select(b => new BusquedaTesoroResumenDto
            {
                Id = b.Id,
                Nombre = b.Nombre,
                Descripcion = b.Descripcion,
                Estado = ((EstadoBusqueda)b.Estado).ToString(),
                TotalEtapas = b.Etapas.Count,
                FechaCreacion = b.FechaCreacion
            })
            .ToListAsync(cancelacion);
    }

    public async Task AgregarEtapaAsync(Guid busquedaId, Etapa etapa, CancellationToken cancelacion)
    {
        var modelo = BusquedasMapeador.AModelo(etapa);
        _contexto.Etapas.Add(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ModificarEtapaAsync(Guid busquedaId, Etapa etapa, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Etapas
            .FirstOrDefaultAsync(e => e.Id == etapa.Id, cancelacion);
        if (modelo is null) return;

        modelo.Titulo = etapa.Titulo;
        modelo.Descripcion = etapa.Descripcion;
        modelo.Orden = etapa.Orden;
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task EliminarEtapaAsync(Guid busquedaId, Guid etapaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Etapas
            .FirstOrDefaultAsync(e => e.Id == etapaId, cancelacion);
        if (modelo is null) return;

        _contexto.Etapas.Remove(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task AgregarMisionAsync(Guid etapaId, Mision mision, CancellationToken cancelacion)
    {
        var modelo = BusquedasMapeador.AModelo(mision);
        _contexto.Misiones.Add(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ModificarMisionAsync(Guid etapaId, Mision mision, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Misiones
            .FirstOrDefaultAsync(m => m.Id == mision.Id, cancelacion);
        if (modelo is null) return;

        modelo.Titulo = mision.Titulo;
        modelo.Descripcion = mision.Descripcion;
        modelo.Tipo = (int)mision.Tipo;
        modelo.PistaClave = mision.PistaClave;
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ModificarPistaAsync(Pista pista, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Pistas
            .FirstOrDefaultAsync(p => p.Id == pista.Id, cancelacion);
        if (modelo is null) return;

        modelo.Contenido = pista.Contenido;
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task EliminarPistaAsync(Guid etapaId, Guid pistaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Pistas
            .FirstOrDefaultAsync(p => p.Id == pistaId, cancelacion);
        if (modelo is null) return;

        _contexto.Pistas.Remove(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task AgregarPistaAsync(Guid etapaId, Pista pista, CancellationToken cancelacion)
    {
        var modelo = BusquedasMapeador.AModelo(pista);
        _contexto.Pistas.Add(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task EliminarMisionAsync(Guid etapaId, Guid misionId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Misiones
            .FirstOrDefaultAsync(m => m.Id == misionId, cancelacion);
        if (modelo is null) return;

        _contexto.Misiones.Remove(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ActivarBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion)
    {
        var modelo = await _contexto.BusquedasTesoro
            .FirstOrDefaultAsync(b => b.Id == busqueda.Id, cancelacion);
        if (modelo is null) return;

        modelo.Estado = (int)EstadoBusqueda.Activa;

        _contexto.EventosSalida.Add(new EventoSalidaModelo
        {
            Id = Guid.NewGuid(),
            Tipo = "BusquedaTesoroActivada",
            Datos = System.Text.Json.JsonSerializer.Serialize(new
            {
                BusquedaId = busqueda.Id,
                busqueda.Nombre,
                CantidadEtapas = busqueda.Etapas.Count
            }),
            FechaCreacion = DateTime.UtcNow,
            Procesado = false
        });

        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ArchivarBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion)
    {
        var modelo = await _contexto.BusquedasTesoro
            .FirstOrDefaultAsync(b => b.Id == busqueda.Id, cancelacion);
        if (modelo is null) return;

        modelo.Estado = (int)EstadoBusqueda.Inactiva;

        _contexto.EventosSalida.Add(new EventoSalidaModelo
        {
            Id = Guid.NewGuid(),
            Tipo = "BusquedaTesoroDesactivada",
            Datos = System.Text.Json.JsonSerializer.Serialize(new { BusquedaId = busqueda.Id }),
            FechaCreacion = DateTime.UtcNow,
            Procesado = false
        });

        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task<List<BusquedaTesoroResumenDto>> ObtenerBusquedasActivasAsync(CancellationToken cancelacion)
    {
        var estadoActiva = (int)EstadoBusqueda.Activa;

        return await _contexto.BusquedasTesoro
            .AsNoTracking()
            .Where(b => b.Estado == estadoActiva)
            .OrderBy(b => b.Nombre)
            .Select(b => new BusquedaTesoroResumenDto
            {
                Id = b.Id,
                Nombre = b.Nombre,
                Descripcion = b.Descripcion,
                Estado = nameof(EstadoBusqueda.Activa),
                TotalEtapas = b.Etapas.Count,
                FechaCreacion = b.FechaCreacion
            })
            .ToListAsync(cancelacion);
    }

    public async Task<BusquedaTesoroDetalleDto?> ObtenerDetalleBusquedaAsync(
        Guid busquedaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.BusquedasTesoro
            .Include(b => b.Etapas.OrderBy(e => e.Orden))
                .ThenInclude(e => e.Misiones)
            .Include(b => b.Etapas)
                .ThenInclude(e => e.Pistas)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == busquedaId, cancelacion);

        if (modelo is null) return null;

        return new BusquedaTesoroDetalleDto
        {
            Id = modelo.Id,
            Nombre = modelo.Nombre,
            Descripcion = modelo.Descripcion,
            Estado = ((EstadoBusqueda)modelo.Estado).ToString(),
            FechaCreacion = modelo.FechaCreacion,
            Etapas = modelo.Etapas.Select(e => new EtapaDetalleDto
            {
                Id = e.Id,
                Titulo = e.Titulo,
                Descripcion = e.Descripcion,
                Orden = e.Orden,
                Misiones = e.Misiones.Select(m => new MisionDetalleDto
                {
                    Id = m.Id,
                    Titulo = m.Titulo,
                    Descripcion = m.Descripcion,
                    Tipo = ((TipoMision)m.Tipo).ToString(),
                    PistaClave = m.PistaClave
                }).ToList(),
                Pistas = e.Pistas.Select(p => new PistaDetalleDto
                {
                    Id = p.Id,
                    Contenido = p.Contenido
                }).ToList()
            }).ToList()
        };
    }
}
