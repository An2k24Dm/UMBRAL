namespace IdentidadServicio.Aplicacion.Puertos;

// Puerto dedicado al control de la bandera DebeCambiarContrasena (UMBRAL).
// Es independiente de los repositorios de agregado para mantener la
// responsabilidad acotada y no ensuciar la entidad de dominio Usuario con
// metadatos puramente de persistencia.
//
// Solo aplica al flujo administrativo de Operador/Administrador. El
// Participante nunca se marca como debe-cambiar-contraseña.
public interface IRepositorioControlContrasenaTemporal
{
    // Devuelve true si el usuario debe cambiar su contraseña en el próximo
    // login. Si no existe el usuario o no es Operador/Administrador devuelve
    // false (no aplicamos el flujo a Participantes).
    Task<bool> ObtenerDebeCambiarPorIdKeycloakAsync(
        string idKeycloak, CancellationToken cancelacion);

    // Activa la bandera para el usuario interno indicado por su Id de UMBRAL.
    // Lanza si el usuario no existe o si su rol no es Operador/Administrador.
    Task MarcarDebeCambiarPorIdAsync(Guid idUsuario, CancellationToken cancelacion);

    // Limpia la bandera para el usuario identificado por su IdKeycloak.
    // Idempotente: si ya estaba en false no hace nada.
    Task LimpiarDebeCambiarPorIdKeycloakAsync(
        string idKeycloak, CancellationToken cancelacion);
}
