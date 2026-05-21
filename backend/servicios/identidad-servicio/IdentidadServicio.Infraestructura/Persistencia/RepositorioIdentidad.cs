using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia;

public sealed class RepositorioIdentidad : IRepositorioIdentidad
{
    private readonly ContextoIdentidad _contexto;
    private readonly IdentidadMapeador _mapeador = new();

    public RepositorioIdentidad(ContextoIdentidad contexto)
    {
        _contexto = contexto;
    }

    public async Task<Usuario?> ObtenerPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion)
    {
        var normalizado = nombreUsuario.Trim().ToLowerInvariant();
        var u = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.NombreUsuario == normalizado, cancelacion);
        return u is null ? null : await ReconstruirAsync(u, cancelacion);
    }

    public async Task<Usuario?> ObtenerPorIdKeycloakAsync(string idKeycloak, CancellationToken cancelacion)
    {
        var u = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.IdKeycloak == idKeycloak, cancelacion);
        return u is null ? null : await ReconstruirAsync(u, cancelacion);
    }

    public async Task<bool> ExisteNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion)
    {
        var normalizado = nombreUsuario.Trim().ToLowerInvariant();
        return await _contexto.Usuarios.AsNoTracking()
            .AnyAsync(x => x.NombreUsuario == normalizado, cancelacion);
    }

    public async Task<bool> ExisteCorreoAsync(string correo, CancellationToken cancelacion)
    {
        var normalizado = correo.Trim().ToLowerInvariant();
        return await _contexto.Personas.AsNoTracking()
            .AnyAsync(x => x.Correo == normalizado, cancelacion);
    }

    public async Task<bool> ExisteTelefonoAsync(string telefono, CancellationToken cancelacion)
    {
        var normalizado = telefono.Trim();
        if (normalizado.Length == 0) return false;
        return await _contexto.Personas.AsNoTracking()
            .AnyAsync(x => x.Telefono == normalizado, cancelacion);
    }

    // HU03 — verifica si ya existe un Participante con el alias dado. El alias
    // tiene índice único a nivel de base de datos; esta consulta evita golpear
    // la creación en Keycloak cuando el alias ya está ocupado.
    public async Task<bool> ExisteAliasAsync(string alias, CancellationToken cancelacion)
    {
        var normalizado = alias.Trim();
        if (normalizado.Length == 0) return false;
        return await _contexto.Participantes.AsNoTracking()
            .AnyAsync(x => x.Alias == normalizado, cancelacion);
    }

    // Códigos con formato OP-### / AD-###. Como están zero-padded a 3 dígitos,
    // un orden alfabético descendente coincide con el orden numérico descendente
    // hasta 999 (suficiente para esta etapa).
    public async Task<string?> ObtenerUltimoCodigoOperadorAsync(CancellationToken cancelacion)
    {
        return await _contexto.Operadores.AsNoTracking()
            .Where(o => o.CodigoOperador.StartsWith("OP-"))
            .OrderByDescending(o => o.CodigoOperador)
            .Select(o => o.CodigoOperador)
            .FirstOrDefaultAsync(cancelacion);
    }

    public async Task<string?> ObtenerUltimoCodigoAdministradorAsync(CancellationToken cancelacion)
    {
        return await _contexto.Administradores.AsNoTracking()
            .Where(a => a.CodigoAdministrador != null && a.CodigoAdministrador.StartsWith("AD-"))
            .OrderByDescending(a => a.CodigoAdministrador)
            .Select(a => a.CodigoAdministrador)
            .FirstOrDefaultAsync(cancelacion);
    }

    public async Task GuardarAdministradorAsync(
        Administrador administrador, string idKeycloak, CancellationToken cancelacion)
    {
        var modelos = _mapeador.AModelos(administrador, idKeycloak);
        _contexto.Usuarios.Add(modelos.Usuario);
        _contexto.Personas.Add(modelos.Persona);
        _contexto.Administradores.Add(modelos.Administrador);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task GuardarOperadorAsync(
        Operador operador, string idKeycloak, CancellationToken cancelacion)
    {
        var modelos = _mapeador.AModelos(operador, idKeycloak);
        _contexto.Usuarios.Add(modelos.Usuario);
        _contexto.Personas.Add(modelos.Persona);
        _contexto.Operadores.Add(modelos.Operador);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task GuardarParticipanteAsync(
        Participante participante, string idKeycloak, CancellationToken cancelacion)
    {
        var modelos = _mapeador.AModelos(participante, idKeycloak);
        _contexto.Usuarios.Add(modelos.Usuario);
        _contexto.Personas.Add(modelos.Persona);
        _contexto.Participantes.Add(modelos.Participante);
        await _contexto.SaveChangesAsync(cancelacion);
    }

    // HU07: lista paginada de Participantes para el panel web.
    // Filtra estrictamente por Rol = Participante (deja fuera Operadores y
    // Administradores) y permite ordenar por estado. El orden secundario por
    // NombreUsuario garantiza una paginación estable cuando varias filas
    // comparten el mismo estado.
    public async Task<IReadOnlyList<Participante>> ConsultarParticipantesAsync(
        int pagina, int tamanioPagina, string? ordenEstado, CancellationToken cancelacion)
    {
        if (pagina <= 0) pagina = 1;
        if (tamanioPagina <= 0) tamanioPagina = 10;

        var consulta = _contexto.Usuarios.AsNoTracking()
            .Where(u => u.Rol == (int)RolUsuario.Participante);

        var ordenNormalizado = ordenEstado?.Trim().ToLowerInvariant();
        IOrderedQueryable<UsuarioModelo> ordenada = ordenNormalizado switch
        {
            "asc" => consulta.OrderBy(u => u.Estado).ThenBy(u => u.NombreUsuario),
            "desc" => consulta.OrderByDescending(u => u.Estado).ThenBy(u => u.NombreUsuario),
            _ => consulta.OrderBy(u => u.NombreUsuario)
        };

        var usuarios = await ordenada
            .Skip((pagina - 1) * tamanioPagina)
            .Take(tamanioPagina)
            .ToListAsync(cancelacion);

        if (usuarios.Count == 0) return Array.Empty<Participante>();

        var ids = usuarios.Select(u => u.Id).ToList();
        var personas = await _contexto.Personas.AsNoTracking()
            .Where(p => ids.Contains(p.UsuarioId))
            .ToListAsync(cancelacion);
        var personaIds = personas.Select(p => p.Id).ToList();
        var participantes = await _contexto.Participantes.AsNoTracking()
            .Where(p => personaIds.Contains(p.PersonaId))
            .ToListAsync(cancelacion);

        var resultado = new List<Participante>(usuarios.Count);
        foreach (var u in usuarios)
        {
            var persona = personas.FirstOrDefault(p => p.UsuarioId == u.Id);
            if (persona is null) continue;
            var par = participantes.FirstOrDefault(p => p.PersonaId == persona.Id);
            if (par is null) continue;
            resultado.Add(_mapeador.AParticipante(u, persona, par));
        }

        return resultado;
    }

    public async Task<int> ContarParticipantesAsync(CancellationToken cancelacion)
    {
        return await _contexto.Usuarios.AsNoTracking()
            .CountAsync(u => u.Rol == (int)RolUsuario.Participante, cancelacion);
    }

    public async Task<Participante?> ObtenerParticipantePorIdAsync(
        Guid id, CancellationToken cancelacion)
    {
        var usuario = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancelacion);
        // Si el usuario no existe o no es Participante, no devolvemos nada:
        // HU07 jamás debe exponer Operadores ni Administradores.
        if (usuario is null || usuario.Rol != (int)RolUsuario.Participante) return null;

        var persona = await _contexto.Personas.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id, cancelacion);
        if (persona is null) return null;

        var participante = await _contexto.Participantes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonaId == persona.Id, cancelacion);
        if (participante is null) return null;

        return _mapeador.AParticipante(usuario, persona, participante);
    }

    // HU08 — listado paginado de cuentas internas. Filtra por rol Operador o
    // Administrador (nunca Participante), aplica orden y paginación en SQL y
    // reconstruye las entidades de dominio concretas usando el mapeador. El
    // armado del DTO de respuesta lo hace el manejador del caso de uso.
    public async Task<IReadOnlyList<Usuario>> ConsultarUsuariosInternosAsync(
        int pagina,
        int tamanioPagina,
        RolUsuario? rolFiltro,
        string? ordenEstado,
        CancellationToken cancelacion)
    {
        if (pagina < 1) pagina = 1;
        if (tamanioPagina < 1) tamanioPagina = 10;

        var consulta = ConsultaUsuariosInternosBase(rolFiltro);

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
                // Participantes no entran en HU08; la consulta base ya los excluye.
            }
        }

        return resultado;
    }

    public async Task<int> ContarUsuariosInternosAsync(
        RolUsuario? rolFiltro, CancellationToken cancelacion)
    {
        return await ConsultaUsuariosInternosBase(rolFiltro).CountAsync(cancelacion);
    }

    // Consulta base para HU08: parte de Usuarios filtrando estrictamente por
    // rol Operador / Administrador y aplica el filtro opcional de rol.
    private IQueryable<UsuarioModelo> ConsultaUsuariosInternosBase(RolUsuario? rolFiltro)
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

    public async Task<Usuario?> ObtenerUsuarioInternoPorIdAsync(Guid id, CancellationToken cancelacion)
    {
        var u = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancelacion);
        if (u is null) return null;

        // HU08 sólo expone Operadores y Administradores como "usuarios internos".
        var rol = (RolUsuario)u.Rol;
        if (rol != RolUsuario.Administrador && rol != RolUsuario.Operador)
            return null;

        return await ReconstruirAsync(u, cancelacion);
    }

    private async Task<Usuario?> ReconstruirAsync(UsuarioModelo u, CancellationToken cancelacion)
    {
        var persona = await _contexto.Personas.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UsuarioId == u.Id, cancelacion);
        if (persona is null) return null;

        return (RolUsuario)u.Rol switch
        {
            RolUsuario.Administrador => await ReconstruirAdminAsync(u, persona, cancelacion),
            RolUsuario.Operador      => await ReconstruirOperadorAsync(u, persona, cancelacion),
            RolUsuario.Participante  => await ReconstruirParticipanteAsync(u, persona, cancelacion),
            _ => null
        };
    }

    private async Task<Usuario?> ReconstruirAdminAsync(UsuarioModelo u, PersonaModelo p, CancellationToken c)
    {
        var a = await _contexto.Administradores.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PersonaId == p.Id, c);
        return a is null ? null : _mapeador.AAdministrador(u, p, a);
    }

    private async Task<Usuario?> ReconstruirOperadorAsync(UsuarioModelo u, PersonaModelo p, CancellationToken c)
    {
        var o = await _contexto.Operadores.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PersonaId == p.Id, c);
        return o is null ? null : _mapeador.AOperador(u, p, o);
    }

    private async Task<Usuario?> ReconstruirParticipanteAsync(UsuarioModelo u, PersonaModelo p, CancellationToken c)
    {
        var par = await _contexto.Participantes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PersonaId == p.Id, c);
        return par is null ? null : _mapeador.AParticipante(u, p, par);
    }
}
