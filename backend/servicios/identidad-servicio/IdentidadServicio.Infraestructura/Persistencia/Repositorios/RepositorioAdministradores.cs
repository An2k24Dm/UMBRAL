using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Infraestructura.Persistencia.Mapeadores;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

public sealed class RepositorioAdministradores : IRepositorioAdministradores
{
    private readonly ContextoIdentidad _contexto;
    private readonly IdentidadMapeador _mapeador;

    public RepositorioAdministradores(ContextoIdentidad contexto)
    {
        _contexto = contexto;
        _mapeador = new IdentidadMapeador();
    }

    public Task AgregarAsync(
        Administrador administrador, string idKeycloak, CancellationToken cancelacion)
    {
        var modelos = _mapeador.AModelos(administrador, idKeycloak);
        _contexto.Usuarios.Add(modelos.Usuario);
        _contexto.Personas.Add(modelos.Persona);
        _contexto.Administradores.Add(modelos.Administrador);
        return Task.CompletedTask;
    }

    public async Task<string?> ObtenerUltimoCodigoAsync(CancellationToken cancelacion)
    {
        return await _contexto.Administradores.AsNoTracking()
            .Where(a => a.CodigoAdministrador != null && a.CodigoAdministrador.StartsWith("AD-"))
            .OrderByDescending(a => a.CodigoAdministrador)
            .Select(a => a.CodigoAdministrador)
            .FirstOrDefaultAsync(cancelacion);
    }
}
