using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioUsuariosLectura : IRepositorioUsuariosLectura
{
    private readonly ContextoIdentidad _contexto;
    private readonly ReconstructorAgregadoUsuario _reconstructor;
    private readonly IdentidadMapeador _mapeador;

    public RepositorioUsuariosLectura(ContextoIdentidad contexto)
    {
        _contexto = contexto;
        _mapeador = new IdentidadMapeador();
        _reconstructor = new ReconstructorAgregadoUsuario(contexto, _mapeador);
    }

    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(
        string nombreUsuario, CancellationToken cancelacion)
    {
        var normalizado = nombreUsuario.Trim().ToLowerInvariant();
        var u = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NombreUsuario == normalizado, cancelacion);
        return u is null ? null : await _reconstructor.ReconstruirAsync(u, cancelacion);
    }

    public async Task<Usuario?> ObtenerPorIdKeycloakAsync(
        string idKeycloak, CancellationToken cancelacion)
    {
        var u = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdKeycloak == idKeycloak, cancelacion);
        return u is null ? null : await _reconstructor.ReconstruirAsync(u, cancelacion);
    }

    public async Task<IReadOnlyList<Usuario>> ConsultarUsuariosInternosAsync(
        int pagina,
        int tamanioPagina,
        RolUsuario? rolFiltro,
        string? ordenEstado,
        CancellationToken cancelacion)
    {
        if (pagina < 1) pagina = 1;
        if (tamanioPagina < 1) tamanioPagina = 10;

        var consulta = ConsultaInternosBase(rolFiltro);

        // Orden estable: estado opcional + nombre de usuario como desempate.
        var ordenada = ordenEstado?.Trim().ToLowerInvariant() switch
        {
            "asc"  => consulta.OrderBy(u => u.Estado).ThenBy(u => u.NombreUsuario),
            "desc" => consulta.OrderByDescending(u => u.Estado).ThenBy(u => u.NombreUsuario),
            _      => consulta.OrderBy(u => u.NombreUsuario)
        };

        var usuarios = await ordenada
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .ToListAsync(cancelacion);

        if (usuarios.Count == 0) return Array.Empty<Usuario>();

        var ids = usuarios.Select(u => u.Id).ToList();
        var personas = await _contexto.Personas.AsNoTracking()
            .Where(p => ids.Contains(p.UsuarioId))
            .ToListAsync(cancelacion);
        var personaIds = personas.Select(p => p.Id).ToList();
        var administradores = await _contexto.Administradores.AsNoTracking()
            .Where(a => personaIds.Contains(a.PersonaId))
            .ToListAsync(cancelacion);
        var operadores = await _contexto.Operadores.AsNoTracking()
            .Where(o => personaIds.Contains(o.PersonaId))
            .ToListAsync(cancelacion);

        var resultado = new List<Usuario>(usuarios.Count);
        foreach (var u in usuarios)
        {
            var persona = personas.FirstOrDefault(p => p.UsuarioId == u.Id);
            if (persona is null) continue;

            switch ((RolUsuario)u.Rol)
            {
                case RolUsuario.Administrador:
                {
                    var a = administradores.FirstOrDefault(x => x.PersonaId == persona.Id);
                    if (a is null) continue;
                    resultado.Add(_mapeador.AAdministrador(u, persona, a));
                    break;
                }
                case RolUsuario.Operador:
                {
                    var o = operadores.FirstOrDefault(x => x.PersonaId == persona.Id);
                    if (o is null) continue;
                    resultado.Add(_mapeador.AOperador(u, persona, o));
                    break;
                }
            }
        }

        return resultado;
    }

    public async Task<int> ContarUsuariosInternosAsync(
        RolUsuario? rolFiltro, CancellationToken cancelacion)
    {
        return await ConsultaInternosBase(rolFiltro).CountAsync(cancelacion);
    }

    public async Task<Usuario?> ObtenerUsuarioInternoPorIdAsync(
        Guid id, CancellationToken cancelacion)
    {
        var u = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancelacion);
        if (u is null) return null;

        var rol = (RolUsuario)u.Rol;
        if (rol != RolUsuario.Administrador && rol != RolUsuario.Operador)
            return null;

        return await _reconstructor.ReconstruirAsync(u, cancelacion);
    }

    public async Task<string?> ObtenerIdKeycloakUsuarioInternoAsync(
        Guid id, CancellationToken cancelacion)
    {
        var rolAdmin = (int)RolUsuario.Administrador;
        var rolOperador = (int)RolUsuario.Operador;
        return await _contexto.Usuarios.AsNoTracking()
            .Where(u => u.Id == id && (u.Rol == rolAdmin || u.Rol == rolOperador))
            .Select(u => u.IdKeycloak)
            .FirstOrDefaultAsync(cancelacion);
    }

    public async Task<IReadOnlyList<Guid>> FiltrarAdministradoresPorIdsAsync(
        IReadOnlyCollection<Guid> usuariosIds, CancellationToken cancelacion)
    {
        if (usuariosIds is null || usuariosIds.Count == 0)
            return Array.Empty<Guid>();

        var rolAdmin = (int)RolUsuario.Administrador;
        var idsKeycloakBuscados = usuariosIds.Select(g => g.ToString()).ToList();

        var idsKeycloakEncontrados = await _contexto.Usuarios.AsNoTracking()
            .Where(u => u.Rol == rolAdmin && idsKeycloakBuscados.Contains(u.IdKeycloak))
            .Select(u => u.IdKeycloak)
            .ToListAsync(cancelacion);

        return idsKeycloakEncontrados
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();
    }

    private IQueryable<UsuarioModelo> ConsultaInternosBase(RolUsuario? rolFiltro)
    {
        var rolAdmin = (int)RolUsuario.Administrador;
        var rolOperador = (int)RolUsuario.Operador;

        var consulta = _contexto.Usuarios.AsNoTracking()
            .Where(u => u.Rol == rolAdmin || u.Rol == rolOperador);

        if (rolFiltro is RolUsuario.Administrador)
            consulta = consulta.Where(u => u.Rol == rolAdmin);
        else if (rolFiltro is RolUsuario.Operador)
            consulta = consulta.Where(u => u.Rol == rolOperador);

        return consulta;
    }
}
