using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Dominio.Enums;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Aplicacion.Fabricas;

public sealed class FabricaEstrategiaCreacionUsuario
{
    private readonly IEnumerable<IEstrategiaCreacionUsuario> _estrategias;

    public FabricaEstrategiaCreacionUsuario(IEnumerable<IEstrategiaCreacionUsuario> estrategias)
    {
        _estrategias = estrategias;
    }

    public IEstrategiaCreacionUsuario Obtener(RolUsuario rol)
    {
        if (!Enum.IsDefined(typeof(RolUsuario), rol))
        {
            throw new RolNoValidoExcepcion();
        }

        return _estrategias.FirstOrDefault(e => e.PuedeCrear(rol))
               ?? throw new RolNoValidoExcepcion();
    }
}
