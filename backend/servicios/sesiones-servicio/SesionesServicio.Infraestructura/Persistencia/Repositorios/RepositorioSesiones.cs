using Microsoft.EntityFrameworkCore;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioSesiones : IRepositorioSesiones
{
    // Estados que cuentan como "vigentes" para bloquear la desactivación
    // de contenido en juegos-servicio. Finalizada y Cancelada NO entran.
    private static readonly EstadoSesion[] EstadosVigentes =
    {
        EstadoSesion.Programada,
        EstadoSesion.EnPreparacion,
        EstadoSesion.Activa,
        EstadoSesion.Pausada
    };

    private readonly ContextoSesiones _contexto;

    public RepositorioSesiones(ContextoSesiones contexto)
    {
        _contexto = contexto;
    }

    public Task AgregarAsync(Sesion sesion, CancellationToken cancelacion)
    {
        var modelo = SesionesMapeador.HaciaModelo(sesion);
        _contexto.Sesiones.Add(modelo);
        return Task.CompletedTask;
    }

    public async Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Sesiones
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancelacion);
        return modelo is null ? null : SesionesMapeador.HaciaDominio(modelo);
    }

    public async Task<IReadOnlyList<Sesion>> ListarAsync(CancellationToken cancelacion)
    {
        var modelos = await _contexto.Sesiones
            .AsNoTracking()
            .OrderByDescending(s => s.FechaProgramada)
            .ToListAsync(cancelacion);

        return modelos.Select(SesionesMapeador.HaciaDominio).ToList();
    }

    public Task<bool> ExisteSesionVigentePorContenidoAsync(
        TipoJuego tipoJuego, Guid contenidoJuegoId, CancellationToken cancelacion)
    {
        // AnyAsync genera "SELECT 1 ... LIMIT 1": no materializa filas
        // ni cuenta totales, sólo informa existencia. EstadosVigentes
        // se traduce a un IN (...) en PostgreSQL.
        return _contexto.Sesiones
            .AsNoTracking()
            .AnyAsync(s =>
                s.TipoJuego == tipoJuego &&
                s.ContenidoJuegoId == contenidoJuegoId &&
                EstadosVigentes.Contains(s.Estado),
                cancelacion);
    }
}
