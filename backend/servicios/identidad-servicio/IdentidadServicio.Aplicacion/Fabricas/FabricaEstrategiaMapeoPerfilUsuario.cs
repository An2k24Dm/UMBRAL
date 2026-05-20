using IdentidadServicio.Aplicacion.Mapeadores.Perfil;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Aplicacion.Fabricas;

// Recibe el conjunto de estrategias de mapeo de perfil y resuelve la adecuada
// para una instancia concreta de Usuario. El manejador (ObtenerPerfilActual)
// queda libre de cualquier switch sobre el rol o el tipo del usuario.
public sealed class FabricaEstrategiaMapeoPerfilUsuario
{
    private readonly IEnumerable<IEstrategiaMapeoPerfilUsuario> _estrategias;

    public FabricaEstrategiaMapeoPerfilUsuario(IEnumerable<IEstrategiaMapeoPerfilUsuario> estrategias)
    {
        _estrategias = estrategias;
    }

    public PerfilUsuarioDto Mapear(Usuario usuario)
    {
        var estrategia = _estrategias.FirstOrDefault(e => e.PuedeMapear(usuario))
                         ?? throw new RolNoValidoExcepcion();
        return estrategia.Mapear(usuario);
    }
}
