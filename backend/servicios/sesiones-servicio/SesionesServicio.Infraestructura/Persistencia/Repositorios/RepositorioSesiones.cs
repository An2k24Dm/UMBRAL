using Microsoft.EntityFrameworkCore;
using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Dominio.Abstract;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Enums;
using SesionesServicio.Infraestructura.Persistencia.Mapeadores;

namespace SesionesServicio.Infraestructura.Persistencia.Repositorios;

// Implementa ambos puertos: el repositorio del agregado (dominio) y las
// consultas de lectura (aplicación). Compartir la implementación evita
// duplicar el acceso a EF, manteniendo separadas las dependencias que ven
// los casos de uso.
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

        // Copia los escalares (incluye tipo_sesion y capacidad) sin tocar la
        // clave ni las navegaciones.
        _contexto.Entry(existente).CurrentValues.SetValues(actualizado);

        // Reemplazo de colecciones hijas fijando el estado explícitamente vía
        // los DbSet. No se manipulan las navegaciones para evitar que EF infiera
        // el estado: como la clave es ValueGeneratedOnAdd y los hijos nuevos ya
        // traen un Guid asignado, al agregarlos por navegación EF los tomaría
        // como "existentes" (Modified) y fallaría. Add explícito fuerza Added.
        _contexto.SesionMisiones.RemoveRange(existente.Misiones);
        _contexto.Equipos.RemoveRange(existente.Equipos);
        _contexto.Participantes.RemoveRange(existente.Participantes);

        _contexto.SesionMisiones.AddRange(actualizado.Misiones);
        _contexto.Equipos.AddRange(actualizado.Equipos);
        _contexto.Participantes.AddRange(actualizado.Participantes);
    }

    public async Task EliminarAsync(Sesion sesion, CancellationToken cancelacion)
    {
        // Se localiza con tracking (sin AsNoTracking) e incluyendo las
        // colecciones hijas para poder borrarlas en el mismo SaveChanges.
        var existente = await _contexto.Sesiones
            .Include(s => s.Misiones)
            .Include(s => s.Equipos)
            .Include(s => s.Participantes)
            .FirstOrDefaultAsync(s => s.Id == sesion.Id, cancelacion);

        if (existente is null)
            return;

        // Borrado explícito de las filas LOCALES del microservicio de sesiones
        // (determinístico también con el proveedor InMemory de las pruebas). Se
        // eliminan las relaciones/inscripciones locales, NO los datos maestros:
        //   * SesionMision guarda mision_id (referencia a juegos-servicio).
        //   * Participante guarda participante_identidad_id (referencia a
        //     identidad-servicio).
        // Esos microservicios no se tocan: aquí solo se borran filas de las
        // tablas sesiones."SesionMision", "Participante", "Equipo" y "Sesion".
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

        return modelos.Select(_mapeador.HaciaDominio).ToList();
    }
}
