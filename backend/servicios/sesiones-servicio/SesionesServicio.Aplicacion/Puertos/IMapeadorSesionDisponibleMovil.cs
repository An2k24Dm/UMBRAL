using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Puertos;

// Strategy de mapeo de una Sesion concreta a SesionDisponibleMovilDto
// (capacidades de participantes/equipos según el tipo).
public interface IMapeadorSesionDisponibleMovil
{
    bool Soporta(Sesion sesion);
    SesionDisponibleMovilDto Mapear(Sesion sesion);
}
