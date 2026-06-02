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
            .Include(b => b.Mision)
                .ThenInclude(m => m!.Pistas)
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
                TieneMision = b.Mision != null,
                FechaCreacion = b.FechaCreacion
            })
            .ToListAsync(cancelacion);
    }

    public async Task AsignarMisionAsync(Guid busquedaId, Mision mision, CancellationToken cancelacion)
    {
        var modelo = BusquedasMapeador.AModelo(mision);
        _contexto.Misiones.Add(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task ModificarMisionAsync(Mision mision, CancellationToken cancelacion)
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

    public async Task EliminarMisionAsync(Guid busquedaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Misiones
            .FirstOrDefaultAsync(m => m.BusquedaId == busquedaId, cancelacion);
        if (modelo is null) return;

        _contexto.Misiones.Remove(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task AgregarPistaAsync(Guid misionId, Pista pista, CancellationToken cancelacion)
    {
        var modelo = BusquedasMapeador.AModelo(pista);
        _contexto.Pistas.Add(modelo);
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

    public async Task EliminarPistaAsync(Guid pistaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Pistas
            .FirstOrDefaultAsync(p => p.Id == pistaId, cancelacion);
        if (modelo is null) return;

        _contexto.Pistas.Remove(modelo);
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
                busqueda.Nombre
            }),
            FechaCreacion = DateTime.UtcNow,
            Procesado = false
        });

        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task EliminarBusquedaTesoroAsync(Guid busquedaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.BusquedasTesoro
            .FirstOrDefaultAsync(b => b.Id == busquedaId, cancelacion);
        if (modelo is null) return;

        _contexto.BusquedasTesoro.Remove(modelo);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task DesactivarBusquedaTesoroAsync(BusquedaTesoro busqueda, CancellationToken cancelacion)
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
                TieneMision = b.Mision != null,
                FechaCreacion = b.FechaCreacion
            })
            .ToListAsync(cancelacion);
    }

    public async Task<BusquedaTesoroDetalleDto?> ObtenerDetalleBusquedaAsync(
        Guid busquedaId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.BusquedasTesoro
            .Include(b => b.Mision)
                .ThenInclude(m => m!.Pistas)
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
            Mision = modelo.Mision is null ? null : new MisionDetalleDto
            {
                Id = modelo.Mision.Id,
                Titulo = modelo.Mision.Titulo,
                Descripcion = modelo.Mision.Descripcion,
                Tipo = ((TipoMision)modelo.Mision.Tipo).ToString(),
                PistaClave = modelo.Mision.PistaClave,
                Pistas = modelo.Mision.Pistas.Select(p => new PistaDetalleDto
                {
                    Id = p.Id,
                    Contenido = p.Contenido
                }).ToList()
            }
        };
    }
}
