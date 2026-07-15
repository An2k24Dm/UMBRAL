using IdentidadServicio.Aplicacion.Puertos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Infraestructura.Persistencia.Mapeadores;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

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
        return await ReconstruirSiEsParticipanteAsync(usuario, cancelacion);
    }

    public async Task<Participante?> ObtenerPorIdKeycloakAsync(
        string idKeycloak, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(idKeycloak)) return null;
        var usuario = await _contexto.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdKeycloak == idKeycloak, cancelacion);
        return await ReconstruirSiEsParticipanteAsync(usuario, cancelacion);
    }

    public async Task<string> ActualizarAsync(Participante participante, CancellationToken cancelacion)
    {
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Id == participante.Id, cancelacion)
            ?? throw new InvalidOperationException(
                $"El usuario {participante.Id} no existe en base de datos.");

        if (usuario.Rol != (int)RolUsuario.Participante)
            throw new InvalidOperationException(
                "Sólo se puede actualizar mediante este método a usuarios con rol Participante.");

        var persona = await _contexto.Personas
            .FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id, cancelacion)
            ?? throw new InvalidOperationException(
                $"El usuario {participante.Id} no tiene fila Persona asociada.");

        usuario.NombreUsuario = participante.NombreUsuario.Valor;

        persona.Nombre = participante.NombrePersona.Nombre;
        persona.Apellido = participante.NombrePersona.Apellido;
        persona.Correo = participante.Correo.Valor;
        persona.Direccion = participante.DatosContacto.Direccion;
        persona.Telefono = participante.DatosContacto.Telefono;
        persona.Sexo = (int)participante.Sexo;
        persona.FechaNacimiento = participante.FechaNacimiento;

        // HU10 — alias del Participante vive en la tabla Participante. Se
        // localiza por PersonaId; FechaRegistro NO se reescribe.
        var participanteModelo = await _contexto.Participantes
            .FirstOrDefaultAsync(p => p.PersonaId == persona.Id, cancelacion)
            ?? throw new InvalidOperationException(
                $"El Participante {participante.Id} no tiene fila Participante asociada.");
        participanteModelo.Alias = participante.Alias;

        return usuario.IdKeycloak;
    }

    public async Task EliminarAsync(Participante participante, CancellationToken cancelacion)
    {
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Id == participante.Id, cancelacion)
            ?? throw new InvalidOperationException(
                $"El usuario {participante.Id} no existe en base de datos.");

        if (usuario.Rol != (int)RolUsuario.Participante)
            throw new InvalidOperationException(
                "Sólo se puede eliminar mediante este método a usuarios con rol Participante.");

        _contexto.Usuarios.Remove(usuario);
    }

    public async Task ActualizarEstadoAsync(Participante participante, CancellationToken cancelacion)
    {
        var usuario = await _contexto.Usuarios
            .FirstOrDefaultAsync(u => u.Id == participante.Id, cancelacion)
            ?? throw new InvalidOperationException(
                $"El usuario {participante.Id} no existe en base de datos.");
        if (usuario.Rol != (int)RolUsuario.Participante)
            throw new InvalidOperationException(
                "Sólo se puede cambiar el estado mediante este método a usuarios con rol Participante.");
        usuario.Estado = (int)participante.Estado;
    }

    private async Task<Participante?> ReconstruirSiEsParticipanteAsync(
        UsuarioModelo? usuario, CancellationToken cancelacion)
    {
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
