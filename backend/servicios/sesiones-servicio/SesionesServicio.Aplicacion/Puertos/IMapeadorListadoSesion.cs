using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IMapeadorListadoSesion
{
    bool Soporta(Sesion sesion);
    SesionListadoDto Mapear(Sesion sesion);
}
