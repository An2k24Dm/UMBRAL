using Microsoft.EntityFrameworkCore;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioSesiones : IRepositorioSesiones
{
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

    public async Task ActualizarAsync(Sesion sesion, CancellationToken cancelacion)
    {
        var existente = await _contexto.Sesiones
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .FirstOrDefaultAsync(s => s.Id == sesion.Id, cancelacion);

        var actualizado = SesionesMapeador.HaciaModelo(sesion);

        if (existente is null)
        {
            _contexto.Sesiones.Add(actualizado);
            return;
        }

        _contexto.Entry(existente).CurrentValues.SetValues(actualizado);

        // Reemplazo de colecciones hijas (alcance actual: la población
        // de equipos/participantes se hace siempre completa).
        _contexto.Participantes.RemoveRange(existente.Participantes);
        _contexto.Equipos.RemoveRange(existente.Equipos);
        _contexto.SesionMisiones.RemoveRange(existente.Misiones);

        existente.Misiones = actualizado.Misiones;
        existente.Equipos = actualizado.Equipos;
        existente.Participantes = actualizado.Participantes;
    }

    public async Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Sesiones
            .AsNoTracking()
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .FirstOrDefaultAsync(s => s.Id == id, cancelacion);
        return modelo is null ? null : SesionesMapeador.HaciaDominio(modelo);
    }

    public async Task<IReadOnlyList<Sesion>> ListarAsync(
        EstadoSesion? estado, Guid? operadorCreadorId, CancellationToken cancelacion)
    {
        var consulta = _contexto.Sesiones
            .AsNoTracking()
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .AsQueryable();

        if (estado.HasValue)
            consulta = consulta.Where(s => s.Estado == estado.Value);

        if (operadorCreadorId.HasValue)
            consulta = consulta.Where(s => s.OperadorCreadorId == operadorCreadorId.Value);

        var modelos = await consulta
            .OrderByDescending(s => s.FechaProgramada)
            .ToListAsync(cancelacion);

        return modelos.Select(SesionesMapeador.HaciaDominio).ToList();
    }

    public async Task<IReadOnlyList<Sesion>> ListarProgramadasVencidasAsync(
        DateTime fechaActualUtc, CancellationToken cancelacion)
    {
        var modelos = await _contexto.Sesiones
            .AsNoTracking()
            .Include(s => s.Misiones)
            .Where(s => s.Estado == EstadoSesion.Programada
                        && s.FechaProgramada <= fechaActualUtc)
            .OrderBy(s => s.FechaProgramada)
            .ToListAsync(cancelacion);

        return modelos.Select(SesionesMapeador.HaciaDominio).ToList();
    }

    public Task<bool> ExisteSesionVigentePorMisionAsync(
        Guid misionId, CancellationToken cancelacion)
    {
        return _contexto.SesionMisiones
            .AsNoTracking()
            .AnyAsync(sm => sm.MisionId == misionId
                            && EstadosVigentes.Contains(
                                _contexto.Sesiones
                                    .Where(s => s.Id == sm.SesionId)
                                    .Select(s => s.Estado)
                                    .FirstOrDefault()),
                cancelacion);
    }

    private static readonly EstadoSesion[] EstadosDisponiblesParticipante =
    {
        EstadoSesion.Programada,
        EstadoSesion.EnPreparacion,
        EstadoSesion.Activa
    };

    public async Task<IReadOnlyList<Sesion>> ListarDisponiblesParaParticipanteAsync(
        string? busqueda, string? tipoSesion, CancellationToken cancelacion)
    {
        var consulta = _contexto.Sesiones
            .AsNoTracking()
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .Where(s => EstadosDisponiblesParticipante.Contains(s.Estado));

        if (!string.IsNullOrWhiteSpace(tipoSesion))
        {
            // Discriminador TPH lógico: la columna `tipo_sesion` guarda
            // "Individual" o "Grupal". Comparación case-insensitive
            // se evalúa en BD vía ILIKE bajo Npgsql.
            consulta = consulta.Where(s =>
                EF.Functions.ILike(s.TipoSesion, tipoSesion));
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var patron = $"%{busqueda.Trim()}%";
            consulta = consulta.Where(s => EF.Functions.ILike(s.Nombre, patron));
        }

        var modelos = await consulta
            .OrderBy(s => s.FechaProgramada)
            .ToListAsync(cancelacion);

        return modelos.Select(SesionesMapeador.HaciaDominio).ToList();
    }
}
