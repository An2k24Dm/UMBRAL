using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Enums;
using Microsoft.EntityFrameworkCore;

namespace IdentidadServicio.Infraestructura.Persistencia.Repositorios;

// Reconstrucción de agregados Usuario/Operador/Administrador/Participante a
// partir de los modelos EF Core. Es la única clase de Infraestructura que
// sabe armar las tres tripletas; los repositorios concretos la usan para no
// duplicar la lógica de "buscar Persona + tabla específica".
internal sealed class ReconstructorAgregadoUsuario
{
    private readonly ContextoIdentidad _contexto;
    private readonly IdentidadMapeador _mapeador;

    public ReconstructorAgregadoUsuario(ContextoIdentidad contexto, IdentidadMapeador mapeador)
    {
        _contexto = contexto;
        _mapeador = mapeador;
    }

    public async Task<Usuario?> ReconstruirAsync(
        UsuarioModelo usuario, CancellationToken cancelacion)
    {
        var persona = await _contexto.Personas.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UsuarioId == usuario.Id, cancelacion);
        if (persona is null) return null;

        return (RolUsuario)usuario.Rol switch
        {
            RolUsuario.Administrador => await ReconstruirAdministradorAsync(usuario, persona, cancelacion),
            RolUsuario.Operador      => await ReconstruirOperadorAsync(usuario, persona, cancelacion),
            RolUsuario.Participante  => await ReconstruirParticipanteAsync(usuario, persona, cancelacion),
            _ => null
        };
    }

    public async Task<Administrador?> ReconstruirAdministradorAsync(
        UsuarioModelo u, PersonaModelo p, CancellationToken c)
    {
        var a = await _contexto.Administradores.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PersonaId == p.Id, c);
        return a is null ? null : _mapeador.AAdministrador(u, p, a);
    }

    public async Task<Operador?> ReconstruirOperadorAsync(
        UsuarioModelo u, PersonaModelo p, CancellationToken c)
    {
        var o = await _contexto.Operadores.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PersonaId == p.Id, c);
        return o is null ? null : _mapeador.AOperador(u, p, o);
    }

    public async Task<Participante?> ReconstruirParticipanteAsync(
        UsuarioModelo u, PersonaModelo p, CancellationToken c)
    {
        var par = await _contexto.Participantes.AsNoTracking()
            .FirstOrDefaultAsync(x => x.PersonaId == p.Id, c);
        return par is null ? null : _mapeador.AParticipante(u, p, par);
    }
}
