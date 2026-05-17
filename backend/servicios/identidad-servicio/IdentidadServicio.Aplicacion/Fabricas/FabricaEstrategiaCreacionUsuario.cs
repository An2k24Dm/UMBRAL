using IdentidadServicio.Aplicacion.Estrategias;
using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Excepciones;

namespace IdentidadServicio.Aplicacion.Fabricas;

public sealed class FabricaEstrategiaCreacionUsuario
{
    private readonly IEnumerable<IEstrategiaCreacionUsuario> _estrategias;

    public FabricaEstrategiaCreacionUsuario(IEnumerable<IEstrategiaCreacionUsuario> estrategias)
    {
        _estrategias = estrategias;
    }

    public IEstrategiaCreacionUsuario Obtener(TipoUsuario tipoUsuario)
    {
        if (!Enum.IsDefined(typeof(TipoUsuario), tipoUsuario))
        {
            throw new RolNoValidoExcepcion();
        }

        return _estrategias.FirstOrDefault(e => e.PuedeCrear(tipoUsuario))
               ?? throw new RolNoValidoExcepcion();
    }
}
