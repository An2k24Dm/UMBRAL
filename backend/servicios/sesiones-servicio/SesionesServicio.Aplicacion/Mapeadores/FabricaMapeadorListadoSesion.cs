using SesionesServicio.Aplicacion.Puertos;
using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Mapeadores;

public sealed class FabricaMapeadorListadoSesion
{
    private readonly IEnumerable<IMapeadorListadoSesion> _mapeadores;

    public FabricaMapeadorListadoSesion(IEnumerable<IMapeadorListadoSesion> mapeadores)
    {
        _mapeadores = mapeadores;
    }

    public SesionListadoDto Mapear(Sesion sesion)
    {
        var mapeador = _mapeadores.FirstOrDefault(m => m.Soporta(sesion))
            ?? throw new SesionInvalidaExcepcion(
                $"No existe un mapeador de listado para el tipo de sesión '{sesion.TipoSesion}'.");
        return mapeador.Mapear(sesion);
    }
}
