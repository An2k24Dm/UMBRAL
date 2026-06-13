using SesionesServicio.Commons.Dtos;
using SesionesServicio.Dominio.Entidades;

namespace SesionesServicio.Aplicacion.Mapeadores;

// Strategy de mapeo de una Sesion concreta a SesionListadoDto (incluye los
// conteos que dependen del tipo: participantes o equipos).
public interface IMapeadorListadoSesion
{
    bool Soporta(Sesion sesion);
    SesionListadoDto Mapear(Sesion sesion);
}
