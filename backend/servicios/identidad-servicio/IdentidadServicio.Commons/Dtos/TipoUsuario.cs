namespace IdentidadServicio.Commons.Dtos;

// Tipo de usuario que el frontend solicita crear. Selecciona la estrategia
// concreta dentro de FabricaEstrategiaCreacionUsuario.
public enum TipoUsuario
{
    Administrador = 1,
    Operador = 2,
    Participante = 3
}
