using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Mapeadores;

public sealed class FabricaMapeadorSesionDisponibleMovil
{
    private readonly IEnumerable<IMapeadorSesionDisponibleMovil> _mapeadores;

    public FabricaMapeadorSesionDisponibleMovil(
        IEnumerable<IMapeadorSesionDisponibleMovil> mapeadores)
    {
        _mapeadores = mapeadores;
    }

    public SesionDisponibleMovilDto Mapear(Sesion sesion)
    {
        var mapeador = _mapeadores.FirstOrDefault(m => m.Soporta(sesion))
            ?? throw new SesionInvalidaExcepcion(
                $"No existe un mapeador disponible-móvil para el tipo de sesión '{sesion.TipoSesion}'.");
        return mapeador.Mapear(sesion);
    }
}
