using Microsoft.EntityFrameworkCore;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioSesiones : IRepositorioSesiones
{
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
}
