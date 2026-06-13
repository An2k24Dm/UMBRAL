using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;
using SesionesServicio.Dominio.Excepciones;

namespace SesionesServicio.Aplicacion.Mapeadores;

// Selecciona el IMapeadorDetalleSesion compatible con la sesión recibida.
// Sin switch/if por tipo concreto: la decisión la encapsula cada estrategia.
public sealed class FabricaMapeadorDetalleSesion
{
    private readonly IEnumerable<IMapeadorDetalleSesion> _mapeadores;

    public FabricaMapeadorDetalleSesion(IEnumerable<IMapeadorDetalleSesion> mapeadores)
    {
        _mapeadores = mapeadores;
    }

    public SesionDetalleDto Mapear(Sesion sesion)
    {
        var mapeador = _mapeadores.FirstOrDefault(m => m.Soporta(sesion))
            ?? throw new SesionInvalidaExcepcion(
                $"No existe un mapeador de detalle para el tipo de sesión '{sesion.TipoSesion}'.");
        return mapeador.Mapear(sesion);
    }
}
