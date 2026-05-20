namespace IdentidadServicio.Aplicacion.Enums;

// Indica desde qué aplicación se está iniciando sesión.
// Determina qué roles pueden autenticarse:
//   Web   → Administrador, Operador
//   Movil → Participante
public enum OrigenInicioSesion
{
    Web = 1,
    Movil = 2
}
