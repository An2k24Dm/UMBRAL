using IdentidadServicio.Aplicacion.Puertos;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioUnicidadUsuario : IRepositorioUnicidadUsuario
{
    private readonly ContextoIdentidad _contexto;

    public RepositorioUnicidadUsuario(ContextoIdentidad contexto)
    {
        _contexto = contexto;
    }

    public async Task<bool> ExisteNombreUsuarioAsync(
        string nombreUsuario, CancellationToken cancelacion)
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

    public async Task<bool> ExisteAliasAsync(string alias, CancellationToken cancelacion)
    {
        var normalizado = alias.Trim();
        if (normalizado.Length == 0) return false;
        return await _contexto.Participantes.AsNoTracking()
            .AnyAsync(x => x.Alias == normalizado, cancelacion);
    }

    public async Task<bool> ExisteNombreUsuarioEnOtroUsuarioAsync(
        string nombreUsuario, Guid idActual, CancellationToken cancelacion)
    {
        var normalizado = nombreUsuario.Trim().ToLowerInvariant();
        return await _contexto.Usuarios.AsNoTracking()
            .AnyAsync(x => x.NombreUsuario == normalizado && x.Id != idActual, cancelacion);
    }

    public async Task<bool> ExisteCorreoEnOtroUsuarioAsync(
        string correo, Guid idActual, CancellationToken cancelacion)
    {
        var normalizado = correo.Trim().ToLowerInvariant();
        return await _contexto.Personas.AsNoTracking()
            .AnyAsync(p => p.Correo == normalizado && p.UsuarioId != idActual, cancelacion);
    }

    public async Task<bool> ExisteTelefonoEnOtroUsuarioAsync(
        string telefono, Guid idActual, CancellationToken cancelacion)
    {
        var normalizado = telefono.Trim();
        if (normalizado.Length == 0) return false;
        return await _contexto.Personas.AsNoTracking()
            .AnyAsync(p => p.Telefono == normalizado && p.UsuarioId != idActual, cancelacion);
    }

    public async Task<bool> ExisteAliasEnOtroUsuarioAsync(
        string alias, Guid idActual, CancellationToken cancelacion)
    {
        var normalizado = alias.Trim();
        if (normalizado.Length == 0) return false;

        var consulta =
            from p in _contexto.Participantes.AsNoTracking()
            join per in _contexto.Personas.AsNoTracking() on p.PersonaId equals per.Id
            where p.Alias == normalizado && per.UsuarioId != idActual
            select p.Id;

        return await consulta.AnyAsync(cancelacion);
    }
}
