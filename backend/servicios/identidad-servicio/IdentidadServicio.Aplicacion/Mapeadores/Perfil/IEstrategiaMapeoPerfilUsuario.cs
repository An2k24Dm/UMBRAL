using IdentidadServicio.Commons.Dtos;
using IdentidadServicio.Dominio.Entidades;

namespace IdentidadServicio.Aplicacion.Mapeadores.Perfil;

// Strategy: una implementación por cada tipo concreto de Usuario.
// Permite agregar nuevos roles sin tocar a las demás estrategias ni al
// manejador de la consulta (OCP). Cada estrategia decide:
//   - PuedeMapear: si la instancia de Usuario corresponde al rol que maneja.
//   - Mapear: produce el DTO derivado adecuado (Administrador/Operador/Participante).
public interface IEstrategiaMapeoPerfilUsuario
{
    bool PuedeMapear(Usuario usuario);
    PerfilUsuarioDto Mapear(Usuario usuario);
}
