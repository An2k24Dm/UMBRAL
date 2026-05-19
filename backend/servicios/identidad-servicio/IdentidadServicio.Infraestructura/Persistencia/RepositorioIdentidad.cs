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
