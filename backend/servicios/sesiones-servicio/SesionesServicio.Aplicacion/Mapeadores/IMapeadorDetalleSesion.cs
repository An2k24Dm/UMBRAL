using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

// Strategy de mapeo de una Sesion concreta a SesionDetalleDto. Cada tipo de
// sesión aporta su propio mapeador. Solo expone métodos.
public interface IMapeadorDetalleSesion
{
    bool Soporta(Sesion sesion);
    SesionDetalleDto Mapear(Sesion sesion);
}
