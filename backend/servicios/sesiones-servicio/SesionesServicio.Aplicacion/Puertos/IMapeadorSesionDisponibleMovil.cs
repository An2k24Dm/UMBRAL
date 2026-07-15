using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Puertos;

public interface IMapeadorSesionDisponibleMovil
{
    bool Soporta(Sesion sesion);
    SesionDisponibleMovilDto Mapear(Sesion sesion);
}
