using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioControlContrasenaTemporal : IRepositorioControlContrasenaTemporal
{
    private readonly ContextoIdentidad _contexto;

    public RepositorioControlContrasenaTemporal(ContextoIdentidad contexto)
    {
        _contexto = contexto;
    }

    public async Task<bool> ObtenerDebeCambiarPorIdKeycloakAsync(
        string idKeycloak, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(idKeycloak)) return false;

        var rolAdmin = (int)RolUsuario.Administrador;
        var rolOperador = (int)RolUsuario.Operador;

        return await _contexto.Usuarios.AsNoTracking()
            .Where(u => u.IdKeycloak == idKeycloak
                        && (u.Rol == rolAdmin || u.Rol == rolOperador))
            .Select(u => u.DebeCambiarContrasena)
            .FirstOrDefaultAsync(cancelacion);
    }

    public async Task MarcarDebeCambiarPorIdAsync(
        Guid idUsuario, CancellationToken cancelacion)
    {
        var rolAdmin = (int)RolUsuario.Administrador;
        var rolOperador = (int)RolUsuario.Operador;

        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Id == idUsuario, cancelacion)
            ?? throw new InvalidOperationException(
                $"No existe usuario {idUsuario} para marcar debe-cambiar-contraseña.");

        if (usuario.Rol != rolAdmin && usuario.Rol != rolOperador)
            throw new InvalidOperationException(
                "Solo Operador o Administrador pueden ser marcados como " +
                "debe-cambiar-contraseña.");

        usuario.DebeCambiarContrasena = true;
        await _contexto.SaveChangesAsync(cancelacion);
    }

    public async Task LimpiarDebeCambiarPorIdKeycloakAsync(
        string idKeycloak, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(idKeycloak)) return;

        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.IdKeycloak == idKeycloak, cancelacion);
        if (usuario is null) return;
        if (!usuario.DebeCambiarContrasena) return;

        usuario.DebeCambiarContrasena = false;
        await _contexto.SaveChangesAsync(cancelacion);
    }
}
