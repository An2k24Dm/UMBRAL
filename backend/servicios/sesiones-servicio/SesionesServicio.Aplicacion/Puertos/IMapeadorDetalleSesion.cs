using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IMapeadorDetalleSesion
{
    bool Soporta(Sesion sesion);
    SesionDetalleDto Mapear(Sesion sesion);
}
