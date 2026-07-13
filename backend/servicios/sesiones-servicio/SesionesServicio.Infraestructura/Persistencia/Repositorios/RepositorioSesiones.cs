using Microsoft.EntityFrameworkCore;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia.Mapeadores;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioSesiones : IRepositorioSesiones, IConsultasSesiones
{
    private static readonly EstadoSesion[] EstadosVigentes =
    {
        EstadoSesion.Programada,
        EstadoSesion.EnPreparacion,
        EstadoSesion.Activa,
        EstadoSesion.Pausada
    };

    private readonly ContextoSesiones _contexto;
    private readonly MapeadorSesionesPersistencia _mapeador;

    public RepositorioSesiones(
        ContextoSesiones contexto,
        MapeadorSesionesPersistencia mapeador)
    {
        _contexto = contexto;
        _mapeador = mapeador;
    }

    public Task AgregarAsync(Sesion sesion, CancellationToken cancelacion)
    {
        var modelo = _mapeador.HaciaModelo(sesion);
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

        var actualizado = _mapeador.HaciaModelo(sesion);

        if (existente is null)
        {
            _contexto.Sesiones.Add(actualizado);
            return;
        }

        _contexto.Entry(existente).CurrentValues.SetValues(actualizado);
        _contexto.SesionMisiones.RemoveRange(existente.Misiones);
        _contexto.Equipos.RemoveRange(existente.Equipos);
        _contexto.Participantes.RemoveRange(existente.Participantes);

        _contexto.SesionMisiones.AddRange(actualizado.Misiones);
        _contexto.Equipos.AddRange(actualizado.Equipos);
        _contexto.Participantes.AddRange(actualizado.Participantes);
    }

    public async Task EliminarAsync(Sesion sesion, CancellationToken cancelacion)
    {
        var existente = await _contexto.Sesiones
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .FirstOrDefaultAsync(s => s.Id == sesion.Id, cancelacion);

        if (existente is null)
            return;
        _contexto.SesionMisiones.RemoveRange(existente.Misiones);
        _contexto.Participantes.RemoveRange(existente.Participantes);
        _contexto.Equipos.RemoveRange(existente.Equipos);
        _contexto.Sesiones.Remove(existente);
    }

    public async Task<Sesion?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Sesiones
            .AsNoTracking()
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .FirstOrDefaultAsync(s => s.Id == id, cancelacion);
        return modelo is null ? null : _mapeador.HaciaDominio(modelo);
    }

    public async Task<Sesion?> ObtenerPorCodigoAsync(
        string codigo, CancellationToken cancelacion)
    {
        var codigoNormalizado = codigo.Trim().ToUpperInvariant();
        var modelo = await _contexto.Sesiones
            .AsNoTracking()
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .FirstOrDefaultAsync(
                s => s.CodigoAcceso.ToUpper() == codigoNormalizado,
                cancelacion);
        return modelo is null ? null : _mapeador.HaciaDominio(modelo);
    }

    public async Task<Sesion?> ObtenerPorEquipoIdAsync(Guid equipoId, CancellationToken cancelacion)
    {
        var modelo = await _contexto.Sesiones
            .AsNoTracking()
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .FirstOrDefaultAsync(s => s.Equipos.Any(e => e.Id == equipoId), cancelacion);
        return modelo is null ? null : _mapeador.HaciaDominio(modelo);
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

        return modelos.Select(_mapeador.HaciaDominio).ToList();
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

        return modelos.Select(_mapeador.HaciaDominio).ToList();
    }

    public async Task<IReadOnlyList<Sesion>> ListarActivasConEtapaVencidaAsync(
        DateTime ahoraUtc, CancellationToken cancelacion)
    {
        var candidatas = await _contexto.Sesiones
            .AsNoTracking()
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .Where(s => s.Estado == EstadoSesion.Activa)
            .ToListAsync(cancelacion);

        return candidatas
            .Select(_mapeador.HaciaDominio)
            .Where(s => s.EjecucionActual is not null
                        && s.EjecucionActual.EstaActiva
                        && s.EjecucionActual.CalcularSegundosRestantes(ahoraUtc) <= 0)
            .ToList();
    }

    public async Task<IReadOnlyList<Sesion>> ListarActivasConPreparacionVencidaAsync(
        DateTime ahoraUtc, CancellationToken cancelacion)
    {
        var candidatas = await _contexto.Sesiones
            .AsNoTracking()
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .Where(s => s.Estado == EstadoSesion.Activa)
            .ToListAsync(cancelacion);

        return candidatas
            .Select(_mapeador.HaciaDominio)
            .Where(s => s.EjecucionActual is not null
                        && s.EjecucionActual.EstaEnPreparacion
                        && s.EjecucionActual.PreparacionVencida(ahoraUtc))
            .ToList();
    }

    public async Task<IReadOnlyList<Sesion>> ListarActivasConCierrePendienteVencidoAsync(
        DateTime ahoraUtc, CancellationToken cancelacion)
    {
        var candidatas = await _contexto.Sesiones
            .AsNoTracking()
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .Where(s => s.Estado == EstadoSesion.Activa)
            .ToListAsync(cancelacion);

        return candidatas
            .Select(_mapeador.HaciaDominio)
            .Where(s => s.EjecucionActual is not null
                        && s.EjecucionActual.EstaEnCierrePendiente
                        && s.EjecucionActual.CierrePendienteVencido(ahoraUtc))
            .ToList();
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
        EstadoSesion.Activa,
        EstadoSesion.Pausada
    };

    private static readonly EstadoSesion[] EstadosBloqueantes =
    {
        EstadoSesion.EnPreparacion,
        EstadoSesion.Activa,
        EstadoSesion.Pausada
    };

    public async Task<SesionParticipacionActivaDto?> ObtenerParticipacionActivaDeParticipanteAsync(
        Guid participanteIdentidadId, CancellationToken cancelacion)
    {
        var fila = await (
            from p in _contexto.Participantes.AsNoTracking()
            join s in _contexto.Sesiones.AsNoTracking() on p.SesionId equals s.Id
            where p.ParticipanteIdentidadId == participanteIdentidadId
                  && EstadosBloqueantes.Contains(s.Estado)
            select new
            {
                s.Id,
                s.Nombre,
                s.Estado,
                s.TipoSesion,
                p.EquipoId
            }).FirstOrDefaultAsync(cancelacion);

        if (fila is null) return null;

        string? equipoNombre = null;
        if (fila.EquipoId is Guid equipoId)
        {
            equipoNombre = await _contexto.Equipos.AsNoTracking()
                .Where(e => e.Id == equipoId)
                .Select(e => e.Nombre)
                .FirstOrDefaultAsync(cancelacion);
        }

        var modo = string.Equals(fila.TipoSesion, ModoSesion.Grupal.ToString(),
            StringComparison.OrdinalIgnoreCase)
            ? ModoSesion.Grupal
            : ModoSesion.Individual;

        return new SesionParticipacionActivaDto(
            fila.Id, fila.Nombre, fila.Estado, modo, fila.EquipoId, equipoNombre);
    }

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

        return modelos.Select(_mapeador.HaciaDominio).ToList();
    }

    public async Task<IReadOnlyList<MiParticipacionProyeccion>> ListarParticipacionesFinalizadasAsync(
        Guid participanteIdentidadId, int limite, CancellationToken cancelacion)
    {
        return await (
            from p in _contexto.Participantes.AsNoTracking()
            join s in _contexto.Sesiones.AsNoTracking() on p.SesionId equals s.Id
            where p.ParticipanteIdentidadId == participanteIdentidadId
                  && s.Estado == EstadoSesion.Finalizada
            orderby s.FechaFinalizacionUtc descending
            select new MiParticipacionProyeccion(
                s.Id,
                s.Nombre,
                s.TipoSesion,
                s.FechaInicioUtc,
                s.FechaFinalizacionUtc,
                (_contexto.RespuestasTrivia
                    .Where(r => r.SesionId == s.Id && r.ParticipanteIdentidadId == participanteIdentidadId)
                    .Sum(r => (int?)r.PuntosGanados) ?? 0) +
                (_contexto.EvidenciasTesoro
                    .Where(e => e.SesionId == s.Id && e.ParticipanteIdentidadId == participanteIdentidadId && e.EsValida)
                    .Sum(e => (int?)e.PuntosGanados) ?? 0)))
            .Take(limite)
            .ToListAsync(cancelacion);
    }
}
