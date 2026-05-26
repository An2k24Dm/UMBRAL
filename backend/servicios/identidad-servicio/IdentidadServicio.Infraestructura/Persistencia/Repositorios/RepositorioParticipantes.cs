using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

// Repositorio específico de Participantes (HU03 registro, HU07 listado/detalle).
public sealed class RepositorioParticipantes : IRepositorioParticipantes
{
    private readonly ContextoIdentidad _contexto;
    private readonly IdentidadMapeador _mapeador;

    public RepositorioParticipantes(ContextoIdentidad contexto)
    {
        _contexto = contexto;
        _mapeador = new IdentidadMapeador();
    }

    public Task AgregarAsync(
        Participante participante, string idKeycloak, CancellationToken cancelacion)
    {
        var modelos = _mapeador.AModelos(participante, idKeycloak);
        _contexto.Usuarios.Add(modelos.Usuario);
        _contexto.Personas.Add(modelos.Persona);
        _contexto.Participantes.Add(modelos.Participante);
        return Task.CompletedTask;
    }

    // HU07 — lista paginada de Participantes. Filtra estrictamente por rol y
    // permite ordenar por estado. Desempate por NombreUsuario para garantizar
    // paginación estable cuando varias filas comparten estado.
    public async Task<IReadOnlyList<Participante>> ConsultarAsync(
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

    public async Task<int> ContarAsync(CancellationToken cancelacion)
    {
        return await _contexto.Usuarios.AsNoTracking()
            .CountAsync(u => u.Rol == (int)RolUsuario.Participante, cancelacion);
    }

    public async Task<Participante?> ObtenerPorIdAsync(Guid id, CancellationToken cancelacion)
    {
        var usuario = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancelacion);
        if (usuario is null || usuario.Rol != (int)RolUsuario.Participante) return null;

        var persona = await _contexto.Personas.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id, cancelacion);
        if (persona is null) return null;

        var participante = await _contexto.Participantes.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PersonaId == persona.Id, cancelacion);
        if (participante is null) return null;

        return _mapeador.AParticipante(usuario, persona, participante);
    }
}
